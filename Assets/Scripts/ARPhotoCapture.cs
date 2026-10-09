// ARPhotoManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Android;
using System.Collections.Generic;

public class ARPhotoManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject capturePanel;
    public GameObject previewPanel;
    public RawImage previewImage;
    public Button captureButton;
    public Button downloadButton;
    public Button retryButton;
    public List<RawImage> photoSlots;

    [Header("Animaciones y efectos")]
    public AudioSource shutterSound;

    private Texture2D lastCapturedPhoto;
    private int nextSlotIndex = 0;

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }

        if (captureButton != null)
            captureButton.onClick.AddListener(() => StartCoroutine(CapturePhoto()));

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                previewPanel.SetActive(false);
                capturePanel.SetActive(true);
            });
        }

        if (downloadButton != null)
            downloadButton.onClick.AddListener(DownloadPhotoFromPreview);

        foreach (var slot in photoSlots)
        {
            RawImage currentSlot = slot;
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OpenPhotoPreview(currentSlot.texture));
        }

        previewPanel.SetActive(false);
        capturePanel.SetActive(true);
    }

    // ============================================================
    // CAPTURA DE FOTO (ahora público para poder invocarse desde
    // RecompensaFinal)
    // ============================================================

    public IEnumerator CapturePhoto()
    {
        Debug.Log("📸 Se hizo clic en el botón de tomar foto");

        yield return new WaitForEndOfFrame();

        Texture2D photo = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        photo.Apply();

        lastCapturedPhoto = photo;

        if (shutterSound != null)
            shutterSound.Play();

        if (nextSlotIndex < photoSlots.Count)
        {
            photoSlots[nextSlotIndex].texture = lastCapturedPhoto;
            nextSlotIndex++;
        }
    }

    // ============================================================
    // PREVISUALIZACIÓN
    // ============================================================

    void OpenPhotoPreview(Texture texture)
    {
        if (texture == null) return;

        previewImage.texture = texture;
        previewPanel.SetActive(true);
        capturePanel.SetActive(false);
        lastCapturedPhoto = texture as Texture2D;
    }

    // ============================================================
    // DESCARGA
    // ============================================================

    void DownloadPhotoFromPreview()
    {
        if (lastCapturedPhoto == null) return;

        string fileName = "ARPhotoPreview_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, lastCapturedPhoto.EncodeToPNG());

#if UNITY_ANDROID
        string galleryDir = "/storage/emulated/0/DCIM/ARPhotos/";
        Directory.CreateDirectory(galleryDir);
        File.Copy(path, Path.Combine(galleryDir, fileName), true);
#endif

        Debug.Log("Foto descargada desde previsualización: " + path);
    }
}