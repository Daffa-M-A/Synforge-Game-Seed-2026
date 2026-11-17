using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0, Pawn = 1, Rook = 2, Knight = 3, Bishop = 4, Queen = 5, King = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;
    public int orderCharge = 0;
    // Nomor urut seumur match: bertambah setiap kali bidak charge (tidak pernah dipadatkan, tanpa batas).
    public int chargeSequence = 0;
    public string pieceName;
    public string roleName;

    public const int AutoReturnOwnerTurns = 3;
    public const int FortressColumnX = 3;
    public bool hasLeftHomeRow = false;
    public int idleOwnerTurns = 0;
    public bool movedThisTurn = false;

    protected Vector3 desiredPosition;
    protected Vector3 desiredScale = Vector3.one;
    protected bool isAnimating = false;
    public bool IsAnimating => isAnimating;

    private const float DefaultHappyDuration = 2.3f;
    public static readonly Vector3 PrisonerFacing = Vector3.left;

    [Header("Jail & Ghost System Data")]
    public int originalX;
    public int originalY;
    public int jailSequenceNumber = 0;
    public bool isJailed = false;
    public int rechargeCountdown = 0;
    public bool isGhost = false;
    protected Animator anim;

    private Coroutine colorFlashRoutine;
    private Coroutine rechargeFlashRoutine;
    private readonly List<RendererFlashEntry> flashEntries = new List<RendererFlashEntry>();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private struct RendererFlashEntry
    {
        public Renderer Renderer;
        public Material[] OriginalSharedMaterials;
        public Material[] FlashMaterials;
    }

    private const float RechargeFlashDuration = 0.5f;
    private const float RechargeFlashInterval = 0.08f;

    private void Awake()
    {
        desiredScale = transform.localScale;
        anim = GetComponentInChildren<Animator>();
    }

    private void OnDestroy()
    {
        ReleaseFlashMaterials();
    }

    private const float PositionSnapThreshold = 0.001f;
    private const float MoveSpeed = 10f;

    private void Update()
    {
        if (!isAnimating)
        {
            if (Vector3.Distance(transform.position, desiredPosition) <= PositionSnapThreshold)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, desiredPosition, MoveSpeed * Time.deltaTime);
            }
        }

        if (Vector3.Distance(transform.localScale, desiredScale) <= PositionSnapThreshold)
        {
            transform.localScale = desiredScale;
        }
        else
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, desiredScale, MoveSpeed * Time.deltaTime);
        }

        UpdateZoneAura();
    }

    // Used for returning the piece after released from jail
    public void SaveOriginalPosition(int x, int y)
    {
        originalX = x;
        originalY = y;
    }

	public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
		{
			List<Vector2Int> r = new List<Vector2Int>();

			if (isJailed || isGhost) return r;

			// Aturan Universal Bentengan: Bisa makan musuh dan bebaskan teman di 8 petak sekitar
			r.AddRange(GetAdjacentCaptures(ref board, tileCountX, tileCountY));
			r.AddRange(GetAdjacentRelease(ref board, ref ChessBoardManager.Instance.prisonerPieces, tileCountX, tileCountY));

			return r;
		}

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force) transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force) transform.localScale = desiredScale;
    }

    public static int GetHomeRow(int pieceTeam)
    {
        return pieceTeam == 0 ? 0 : 15;
    }

    public static int GetEnemyFortressY(int captorTeam)
    {
        return captorTeam == 0 ? 15 : 0;
    }

    public static bool IsEnemyFortressTile(int captorTeam, int x, int y)
    {
        return x == FortressColumnX && y == GetEnemyFortressY(captorTeam);
    }

    public bool IsOnHomeRow()
    {
        return currentY == GetHomeRow(team);
    }

    // ---- Zone Domination ----
    // Papan dibagi dua zona: baris 0-7 = zona tim merah (team 0), baris 8-15 = zona tim biru (team 1).
    public const int ZoneSplitRow = 8;

    public static int GetZoneOwner(int y)
    {
        return y < ZoneSplitRow ? 0 : 1;
    }

    public bool IsInEnemyZone()
    {
        if (isJailed || isGhost) return false;
        return GetZoneOwner(currentY) != team;
    }

    // True kalau di akhir giliran pemiliknya bidak ini sudah berada di zona musuh.
    // Dipakai untuk menghitung "bertahan 1 turn penuh".
    [HideInInspector] public bool heldEnemyZoneAtOwnTurnEnd = false;

    // Aura (glow + asap) saat berada di zona musuh. Dibuat lazy, di-toggle dari Update.
    private GameObject zoneAura;
    private bool zoneAuraShown = false;

    private void UpdateZoneAura()
    {
        bool want = IsInEnemyZone() && !isAnimating;
        if (want == zoneAuraShown) return;
        zoneAuraShown = want;

        if (want && zoneAura == null)
        {
            GameVfxManager vfx = GameVfxManager.EnsureInstance();
            zoneAura = vfx.CreateZoneAura(transform);
        }

        if (zoneAura != null) zoneAura.SetActive(want);
    }

    public bool IsExemptFromAutoReturn()
    {
        if (isJailed || isGhost) return true;
        if (IsOnHomeRow()) return true;
        if (orderCharge == 0 && !hasLeftHomeRow) return true;
        return false;
    }

    public int GetHomeReturnCountdownDisplay()
    {
        if (IsExemptFromAutoReturn()) return 0;

        int remaining = AutoReturnOwnerTurns - idleOwnerTurns;
        return remaining < 1 ? 1 : remaining;
    }

    public void MarkMovedThisTurn()
    {
        movedThisTurn = true;
        idleOwnerTurns = 0;
    }

    public virtual bool CanCapture(ChessPiece enemy)
    {
        if (enemy == null) return false;
        if (isGhost || isJailed) return false;
        if (enemy.isGhost || enemy.isJailed) return false;

        // Bidak yang sedang berada di petak charge (home row, termasuk petak benteng) kebal.
        // Petak benteng yang ditempati bidak musuh berarti terblokir, tidak bisa dimasuki.
        if (enemy.IsOnHomeRow()) return false;

        if (orderCharge < 1 || enemy.orderCharge < 1) return false;
        return orderCharge > enemy.orderCharge;
    }

    // Rules 4: Chess piece can eat if there is an enemy piece adjacent to the piece
    protected List<Vector2Int> GetAdjacentCaptures(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> captures = new List<Vector2Int>();
        Vector2Int[] offsets = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int offset in offsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
            {
                ChessPiece target = board[x, y];
                // Only allow the move if there is an enemy piece
                if (target != null && target.team != team)
                {
                    if (CanCapture(target))
                    {
                        captures.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        return captures;
    }

    // Release if team chess piece is adjacent to the player chess piece
    protected List<Vector2Int> GetAdjacentRelease(ref ChessPiece[,] board, ref ChessPiece[,] prisonerBoard, int tileCountX, int tileCountY)
    {
        List<Vector2Int> libs = new List<Vector2Int>();
        Vector2Int[] offsets = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int offset in offsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
            {
                ChessPiece target = ChessBoardManager.Instance.prisonerPieces[x, y];

                if (target != null && target.team == team && target.isJailed)
                {
                    libs.Add(new Vector2Int(x, y));
                }
            }
        }
        return libs;
    }

    // Check for available tiles in specific direction
    protected List<Vector2Int> GetMovesInDirection(ref ChessPiece[,] board, int tileCountX, int tileCountY, Vector2Int direction)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int x = currentX + direction.x;
        int y = currentY + direction.y;
        int currentDistance = 1; // Penghitung jarak

        // Check if chess piece is still inside the first tile on their own area
        int myBaseRow = (team == 0) ? 0 : 15;
        int maxDistance = (this.currentY == myBaseRow) ? 7 : 16;

        // Rule 1: Stop when player move from the first tile to only 7 tiles
        while (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY && currentDistance <= maxDistance)
        {
            ChessPiece target = board[x, y];

            if (target == null || target.isGhost || target.isJailed)
            {
                moves.Add(new Vector2Int(x, y));
            }
            else
            {
                if (target.team != team)
                {
                    if (CanCapture(target))
                    {
                        moves.Add(new Vector2Int(x, y));
                    }
                }
                break; 
            }
            x += direction.x;
            y += direction.y;
            currentDistance++;
        }
        return moves;
    }

    // Countdown for ghost
    public void TickRechargeCountdown()
    {
        if (!isGhost) return;

        rechargeCountdown--;

        if (rechargeCountdown <= 0)
        {
            isGhost = false;
            rechargeCountdown = 0;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.SfxReSpawn);
        }
    }

    public static Vector3 GetTeamIdleFacing(int pieceTeam)
    {
        return pieceTeam == 0 ? Vector3.forward : Vector3.back;
    }

    protected void SetAnimatorWalking(bool walking)
    {
        if (anim != null) anim.SetBool("isWalking", walking);
    }

    protected void SetAnimatorPrisoner(bool prisoner)
    {
        if (anim != null) anim.SetBool("isPrisoner", prisoner);
    }

    protected void TriggerHappyAnimation()
    {
        if (anim != null) anim.SetTrigger("doHappy");
    }

    protected float GetAnimationClipLength(string clipName, float fallback = DefaultHappyDuration)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return fallback;

        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == clipName) return clips[i].length;
        }

        return fallback;
    }

    public void ApplyPrisonerIdleState(Vector3 jailPosition)
    {
        SetPosition(jailPosition, true);
        transform.rotation = Quaternion.LookRotation(PrisonerFacing);
        SetAnimatorWalking(false);
        SetAnimatorPrisoner(true);
    }

    public void AnimateCaptureToPrison(Vector3 startPos, Vector3 endPos, float speed = 8f, bool sacrificeFlash = false)
    {
        StartCoroutine(CaptureToPrisonCoroutine(startPos, endPos, speed, sacrificeFlash));
    }

    private IEnumerator CaptureToPrisonCoroutine(Vector3 startPos, Vector3 endPos, float speed, bool sacrificeFlash)
    {
        isAnimating = true;
        SetAnimatorPrisoner(false);
        SetAnimatorWalking(false);

        transform.position = startPos;
        desiredPosition = endPos;

        Color? flashColor = sacrificeFlash ? GameVfxManager.Instance.sacrificePrisonBlink : (Color?)null;
        yield return StartCoroutine(MoveToTargetWithWalkAnim(startPos, endPos, speed, true, flashColor));

        SetPosition(endPos, true);
        transform.rotation = Quaternion.LookRotation(PrisonerFacing);
        SetAnimatorWalking(false);
        SetAnimatorPrisoner(true);
        StopColorFlash();

        isAnimating = false;
    }

    // onArrived dipanggil tepat saat bidak hinggap di tile tujuan (sebelum isAnimating dilepas).
    public void MoveWithAnimation(Vector3 startPos, Vector3 endPos, float delay = 0.15f, float speed = 15f, bool sacrificeFlash = false, System.Action onArrived = null)
    {
        StartCoroutine(AnimateMoveCoroutine(startPos, endPos, delay, speed, sacrificeFlash, onArrived));
    }

    private IEnumerator AnimateMoveCoroutine(Vector3 startPos, Vector3 endPos, float delay, float speed, bool sacrificeFlash, System.Action onArrived = null)
    {
        isAnimating = true;
        transform.position = startPos;
        desiredPosition = startPos;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        Color? flashColor = sacrificeFlash ? GameVfxManager.Instance.sacrificeApproachBlink : (Color?)null;
        yield return StartCoroutine(MoveToTargetWithWalkAnim(startPos, endPos, speed, true, flashColor));

        SetPosition(endPos, true);
        SetAnimatorWalking(false);
        StopColorFlash();
        transform.rotation = Quaternion.LookRotation(GetTeamIdleFacing(team));

        onArrived?.Invoke();

        isAnimating = false;
    }

    // Animation for ghost after released from jail
    public void AnimateGhostRelease(Vector3 startPos, Vector3 rallyPos, Vector3 finalPos, float speed = 10f)
    {
        StartCoroutine(GhostReleaseCoroutine(startPos, rallyPos, finalPos, speed));
    }

    private IEnumerator GhostReleaseCoroutine(Vector3 startPos, Vector3 rallyPos, Vector3 finalPos, float speed)
    {
        isAnimating = true;
        SetPosition(startPos, true); // Pastikan posisi awal sinkron

        SetAnimatorWalking(false);
        SetAnimatorPrisoner(false);
        yield return null;

        TriggerHappyAnimation();
        yield return new WaitForSeconds(GetAnimationClipLength("Happy"));

        // Pergerakan ke Rally Point lalu ke Final Point
        yield return StartCoroutine(MoveToTargetWithWalkAnim(startPos, rallyPos, speed, true));
        yield return new WaitForSeconds(0.15f);
        yield return StartCoroutine(MoveToTargetWithWalkAnim(rallyPos, finalPos, speed, true));

        // Snap akhir ke tengah tile
        SetPosition(finalPos, true);
        SetAnimatorWalking(false);
        transform.rotation = Quaternion.LookRotation(GetTeamIdleFacing(team));

        if (GameVfxManager.Instance != null)
        {
            GameVfxManager.Instance.PlayPrisonerReleaseVfx(finalPos, team);
        }

        isAnimating = false;

        // Visual snap: Jika di tile tujuan ada bidak lain, tata agar tidak tumpang tindih
        if (ChessBoardManager.Instance != null)
        {
            ChessBoardManager.Instance.UpdateTileVisuals(currentX, currentY);
        }
    }

    public void AnimateHomeReturn(List<Vector3> worldPath, float speed = 8f)
    {
        StartCoroutine(HomeReturnCoroutine(worldPath, speed));
    }

    private IEnumerator HomeReturnCoroutine(List<Vector3> worldPath, float speed)
    {
        if (worldPath == null || worldPath.Count == 0) yield break;

        isAnimating = true;
        SetAnimatorPrisoner(false);

        Vector3 start = worldPath[0];
        transform.position = start;
        desiredPosition = start;

        for (int i = 1; i < worldPath.Count; i++)
        {
            yield return StartCoroutine(MoveToTargetWithWalkAnim(worldPath[i - 1], worldPath[i], speed, true));
        }

        Vector3 end = worldPath[worldPath.Count - 1];
        SetPosition(end, true);
        SetAnimatorWalking(false);
        transform.rotation = Quaternion.LookRotation(GetTeamIdleFacing(team));
        isAnimating = false;

        if (ChessBoardManager.Instance != null)
        {
            ChessBoardManager.Instance.UpdateTileVisuals(currentX, currentY);
        }
    }

    private IEnumerator MoveToTargetWithWalkAnim(Vector3 start, Vector3 end, float speed, bool spawnDustTrail, Color? flashColor = null)
    {
        Vector3 direction = (end - start).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        SetAnimatorWalking(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLoopSFX("RunSFX");

        if (flashColor.HasValue)
        {
            BeginColorFlash(flashColor.Value);
        }

        float distance = Vector3.Distance(start, end);
        float duration = speed <= 0f ? 0.5f : distance / speed;
        if (duration <= 0f) duration = 0.01f;

        float elapsed = 0f;
        float dustTimer = 0f;
        const float dustInterval = 0.12f;

       while (elapsed < duration)
        {
            Vector3 currentPos = Vector3.Lerp(start, end, elapsed / duration);
            transform.position = currentPos;
            desiredPosition = currentPos; // SINKRONISASI: Agar Update() tidak menarik bidak balik
            
            elapsed += Time.deltaTime;

            if (spawnDustTrail)
            {
                dustTimer += Time.deltaTime;
                if (dustTimer >= dustInterval)
                {
                    dustTimer = 0f;
                    GameVfxManager vfx = GameVfxManager.EnsureInstance();
                    if (vfx != null) vfx.SpawnDustTrail(transform.position);
                }
            }

            yield return null;
        }

        SetPosition(end, true); // Snap terakhir untuk memastikan presisi

        transform.position = end;

        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSFX();
        SetAnimatorWalking(false);

        if (flashColor.HasValue)
        {
            StopColorFlash();
        }
    }

    public void PlayRechargeRankVfx()
    {
        if (rechargeFlashRoutine != null)
        {
            StopCoroutine(rechargeFlashRoutine);
            ClearTint();
            ReleaseFlashMaterials();
        }

        rechargeFlashRoutine = StartCoroutine(RechargeRankFlashCoroutine());
    }

    public bool IsInIdleAnimation()
    {
        if (isAnimating) return false;
        if (anim == null) return true;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        return state.IsName("Idle_Home") || state.IsName("Idle1");
    }

    private IEnumerator RechargeRankFlashCoroutine()
    {
        PrepareFlashEntries();

        if (GameVfxManager.Instance != null)
        {
            GameVfxManager.Instance.PlayRechargeRankVfx(transform.position);
        }

        float elapsed = 0f;
        bool showFlash = false;

        while (elapsed < RechargeFlashDuration)
        {
            showFlash = !showFlash;
            if (showFlash) ApplyTint(GameVfxManager.Instance.rechargeBlink);
            else ClearTint();

            yield return new WaitForSeconds(RechargeFlashInterval);
            elapsed += RechargeFlashInterval;
        }

        ClearTint();
        ReleaseFlashMaterials();
        rechargeFlashRoutine = null;
    }

    public void BeginColorFlash(Color flashColor, float interval = 0.1f)
    {
        StopColorFlash();
        PrepareFlashEntries();
        colorFlashRoutine = StartCoroutine(ColorFlashRoutine(flashColor, interval));
    }

    public void StopColorFlash()
    {
        if (colorFlashRoutine != null)
        {
            StopCoroutine(colorFlashRoutine);
            colorFlashRoutine = null;
        }

        ClearTint();
        ReleaseFlashMaterials();
    }

    // ---- Threat glow (dipakai InputHandler saat hover drag) ----
    // Glow merah steady: hanya emission yang dinaikkan sehingga tekstur asli tetap terlihat
    // (kesan semi transparan). Tidak mengganggu flash lain: flash baru otomatis membatalkan glow.
    private bool threatGlowActive = false;
    public bool IsThreatGlowActive => threatGlowActive;

    public void SetThreatGlow(bool on, Color glowColor, float intensity = 1.5f)
    {
        if (on)
        {
            if (threatGlowActive) return;
            if (colorFlashRoutine != null || rechargeFlashRoutine != null) return;

            PrepareFlashEntries();
            ApplyEmissionGlow(glowColor, intensity);
            threatGlowActive = true;
        }
        else
        {
            if (!threatGlowActive) return;
            threatGlowActive = false;

            // Kalau ada flash lain yang sudah mengambil alih material, jangan disentuh.
            if (colorFlashRoutine != null || rechargeFlashRoutine != null) return;

            ClearTint();
            ReleaseFlashMaterials();
        }
    }

    private void ApplyEmissionGlow(Color glow, float intensity)
    {
        Color emission = glow * intensity;
        emission.a = 1f;

        for (int i = 0; i < flashEntries.Count; i++)
        {
            RendererFlashEntry entry = flashEntries[i];
            if (entry.Renderer == null) continue;

            Material[] flashMats = entry.FlashMaterials;
            for (int m = 0; m < flashMats.Length; m++)
            {
                Material mat = flashMats[m];
                if (mat == null) continue;

                if (mat.HasProperty(EmissionColorId))
                {
                    mat.SetColor(EmissionColorId, emission);
                }
                else
                {
                    // Material tanpa emission: campur warna dasar dengan merah setengah.
                    int id = mat.HasProperty(BaseColorId) ? BaseColorId : mat.HasProperty(ColorId) ? ColorId : -1;
                    if (id != -1) mat.SetColor(id, Color.Lerp(mat.GetColor(id), glow, 0.5f));
                }
            }

            entry.Renderer.materials = flashMats;
        }
    }

    private IEnumerator ColorFlashRoutine(Color flashColor, float interval)
    {
        bool showFlash = false;
        while (true)
        {
            showFlash = !showFlash;
            if (showFlash) ApplyTint(flashColor);
            else ClearTint();
            yield return new WaitForSeconds(interval);
        }
    }

    private void PrepareFlashEntries()
    {
        ReleaseFlashMaterials();
        threatGlowActive = false; // flash apa pun yang baru mengambil alih material membatalkan glow

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;

            Material[] originals = renderer.sharedMaterials;
            Material[] flashMats = new Material[originals.Length];
            for (int m = 0; m < originals.Length; m++)
            {
                if (originals[m] == null) continue;

                flashMats[m] = new Material(originals[m]);
                if (flashMats[m].HasProperty(EmissionColorId))
                {
                    flashMats[m].EnableKeyword("_EMISSION");
                    flashMats[m].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }

            flashEntries.Add(new RendererFlashEntry
            {
                Renderer = renderer,
                OriginalSharedMaterials = originals,
                FlashMaterials = flashMats
            });
        }
    }

    private void ApplyTint(Color tint)
    {
        Color emission = tint * 2.5f;
        emission.a = 1f;

        for (int i = 0; i < flashEntries.Count; i++)
        {
            RendererFlashEntry entry = flashEntries[i];
            if (entry.Renderer == null) continue;

            Material[] flashMats = entry.FlashMaterials;
            for (int m = 0; m < flashMats.Length; m++)
            {
                Material mat = flashMats[m];
                if (mat == null) continue;

                if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, tint);
                else if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, tint);

                if (mat.HasProperty(EmissionColorId)) mat.SetColor(EmissionColorId, emission);
            }

            entry.Renderer.materials = flashMats;
        }
    }

    private void ClearTint()
    {
        for (int i = 0; i < flashEntries.Count; i++)
        {
            RendererFlashEntry entry = flashEntries[i];
            if (entry.Renderer == null) continue;
            entry.Renderer.sharedMaterials = entry.OriginalSharedMaterials;
        }
    }

    private void ReleaseFlashMaterials()
    {
        for (int i = 0; i < flashEntries.Count; i++)
        {
            Material[] flashMats = flashEntries[i].FlashMaterials;
            if (flashMats == null) continue;

            for (int m = 0; m < flashMats.Length; m++)
            {
                if (flashMats[m] != null) Destroy(flashMats[m]);
            }
        }

        flashEntries.Clear();
    }
}