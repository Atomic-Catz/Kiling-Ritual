using UnityEngine;
using PurrNet;
using System.Collections;
using InfimaGames.LowPolyShooterPack;

public class GlobalBuffManager : NetworkBehaviour
{
    public static GlobalBuffManager Instance;

    [Header("Global States")]
    public bool isInstaKillActive { get; private set; } = false;
    public bool isTripleScoreActive { get; private set; } = false;

    private Coroutine instaKillCoroutine;
    private Coroutine tripleScoreCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // INSTA-KILL
    // ==========================================
    public void ActivateInstaKill(float duration)
    {
        if (!isServer) return;
        if (instaKillCoroutine != null) StopCoroutine(instaKillCoroutine);
        instaKillCoroutine = StartCoroutine(InstaKillRoutine(duration));
    }

    private IEnumerator InstaKillRoutine(float duration)
    {
        SyncInstaKillState(true);
        yield return new WaitForSeconds(duration);
        SyncInstaKillState(false);
    }

    [ObserversRpc]
    private void SyncInstaKillState(bool state)
    {
        isInstaKillActive = state;
        Debug.Log(state ? "[GlobalBuff] INSTA-KILL IS ACTIVE FOR EVERYONE!" : "[GlobalBuff] Insta-Kill ended.");
        // TODO: Play Demonic Announcer Voice / UI Icon here!
    }

    // ==========================================
    // TRIPLE SCORE
    // ==========================================
    public void ActivateTripleScore(float duration)
    {
        if (!isServer) return;
        if (tripleScoreCoroutine != null) StopCoroutine(tripleScoreCoroutine);
        tripleScoreCoroutine = StartCoroutine(TripleScoreRoutine(duration));
    }

    private IEnumerator TripleScoreRoutine(float duration)
    {
        SyncTripleScoreState(true);
        yield return new WaitForSeconds(duration);
        SyncTripleScoreState(false);
    }

    [ObserversRpc]
    private void SyncTripleScoreState(bool state)
    {
        isTripleScoreActive = state;
        Debug.Log(state ? "[GlobalBuff] TRIPLE POINTS IS ACTIVE FOR EVERYONE!" : "[GlobalBuff] Triple Points ended.");
        // TODO: Play Announcer Voice / UI Icon here!
    }

    // ==========================================
    // NUKE
    // ==========================================
    public void ActivateNuke()
    {
        if (!isServer) return;
        
        SyncNukeEffect();

        // Find all enemies on the server and kill them
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            EnemyAI enemy = enemyObj.GetComponent<EnemyAI>();
            if (enemy != null && enemy.health > 0)
            {
                enemy.health = 0;
                // Calls your death sequence securely on the server
                enemy.SendMessage("DestroyEnemy", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    [ObserversRpc]
    private void SyncNukeEffect()
    {
        Debug.Log("[GlobalBuff] KABOOM! NUKE ACTIVATED!");
        // TODO: Play Screen Flash / Explosion Sound here!
    }

    // ==========================================
    // CAMOUFLAGE (ZOMBIE BLOOD)
    // ==========================================
    public bool isCamoActive { get; private set; } = false;
    private Coroutine camoCoroutine;

    public void ActivateCamo(float duration)
    {
        if (!isServer) return;
        if (camoCoroutine != null) StopCoroutine(camoCoroutine);
        camoCoroutine = StartCoroutine(CamoRoutine(duration));
    }

    private IEnumerator CamoRoutine(float duration)
    {
        SyncCamoState(true);
        yield return new WaitForSeconds(duration);
        SyncCamoState(false);
    }

    [ObserversRpc]
    private void SyncCamoState(bool state)
    {
        isCamoActive = state;
        Debug.Log(state ? "[GlobalBuff] CAMO IS ACTIVE! Enemies are blind!" : "[GlobalBuff] Camo ended.");
    }
    
    // ==========================================
    // MAX AMMO
    // ==========================================
    public void ActivateMaxAmmo(int amount)
    {
        if (!isServer) return;
        SyncMaxAmmoEffect(amount);
    }

    [ObserversRpc]
    private void SyncMaxAmmoEffect(int amount)
    {
        Debug.Log("[GlobalBuff] MAX AMMO!");
        
        // We find the local player on THIS specific computer and give them ammo.
        // This ensures everyone gets ammo on their own screen without the server having to micro-manage inventories.
        Character[] players = FindObjectsOfType<Character>();
        foreach (var player in players)
        {
            if (player.isOwner)
            {
                WeaponBehaviour[] weapons = player.GetInventory().GetAllWeapons();
                if (weapons != null)
                {
                    foreach (WeaponBehaviour wb in weapons)
                    {
                        if (wb is Weapon weapon && !weapon.IsReserveFull())
                        {
                            weapon.AddReserveAmmunition(amount);
                        }
                    }
                }
            }
        }
    }
}