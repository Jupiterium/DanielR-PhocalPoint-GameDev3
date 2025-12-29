//using UnityEngine;
//using TMPro;
//using System.Collections;

//public class HUDFX : MonoBehaviour
//{
//    [Header("References")]
//    public TMP_Text scoreText; // The main "0 / 3" text
//    public GameObject floatingTextPrefab; // A prefab for the floating "+1"
//    public Transform textSpawnPoint; // Where the "+1" appears (usually beside the score)

//    [Header("3D UI References")]
//    public GameObject[] chessPieceModels; // Drag Pawn, Knight, Bishop models here
//    public Transform hudModelParent; // The parent object moving/spinning in the corner

//    private int currentModelIndex = 0;
//    private Vector3 originalScale;

//    void Start()
//    {
//        // Hide all except the first one
//        if (hudModelParent != null) originalScale = hudModelParent.localScale;

//        for (int i = 0; i < chessPieceModels.Length; i++)
//        {
//            chessPieceModels[i].SetActive(i == 0);
//        }
//    }

//    // Call this from GameManager when an item is collected
//    public void OnItemCollected(int newScore)
//    {
//        // 1. TEXT PUNCH EFFECT
//        StartCoroutine(PunchText(scoreText.transform));

//        // 2. SPAWN "+1" POPUP (Optional)
//        if (floatingTextPrefab && textSpawnPoint)
//        {
//            GameObject popup = Instantiate(floatingTextPrefab, textSpawnPoint.position, Quaternion.identity, textSpawnPoint);
//            Destroy(popup, 1.0f); // Auto destroy after 1 second
//        }

//        // 3. EVOLVE THE CHESS PIECE (Every time you get a point, change the model)
//        // Check if we have a next model available
//        if (currentModelIndex < chessPieceModels.Length - 1)
//        {
//            StartCoroutine(SwapModelSequence(currentModelIndex + 1));
//            currentModelIndex++;
//        }
//    }

//    IEnumerator PunchText(Transform target)
//    {
//        // Scale up
//        float timer = 0;
//        while (timer < 0.1f)
//        {
//            timer += Time.deltaTime;
//            target.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, timer / 0.1f);
//            yield return null;
//        }

//        // Snap back
//        target.localScale = Vector3.one;
//    }

//    IEnumerator SwapModelSequence(int nextIndex)
//    {
//        if (hudModelParent == null) yield break;

//        // 1. Shrink Old
//        float timer = 0;
//        while (timer < 0.15f)
//        {
//            timer += Time.deltaTime;
//            hudModelParent.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / 0.15f);
//            yield return null;
//        }

//        // 2. Swap Active Object
//        chessPieceModels[currentModelIndex].SetActive(false); // Hide Old
//        chessPieceModels[nextIndex].SetActive(true);          // Show New

//        // 3. Grow New (with a little bounce)
//        timer = 0;
//        while (timer < 0.2f)
//        {
//            timer += Time.deltaTime;
//            // A simple "Overshoot" math for bounce
//            float t = timer / 0.2f;
//            float bounce = Mathf.Sin(t * Mathf.PI * 0.5f);
//            hudModelParent.localScale = Vector3.Lerp(Vector3.zero, originalScale, bounce);
//            yield return null;
//        }
//        hudModelParent.localScale = originalScale;
//    }
//}