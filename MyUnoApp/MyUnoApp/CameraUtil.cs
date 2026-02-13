using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using SkiaSharp;

public class CameraUtil
{

    public static async Task<byte[]> TakeSelfieAndResizeAsync(int width, int height)
    {
        byte[] fullImage = await TakeSelfieAsync();
        if (fullImage == null) return null;

        return ResizeImage(fullImage, width, height);
    }

    private static byte[] ResizeImage(byte[] imageData, int width, int height)
    {
        using (var ms = new MemoryStream(imageData))
        using (var bitmap = SKBitmap.Decode(ms))
        {
            // Erstelle ein Ziel-Bitmap
            var info = new SKImageInfo(width, height);
            using (var resized = bitmap.Resize(info, SKFilterQuality.High))
            using (var image = SKImage.FromBitmap(resized))
            using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 80)) // 80% Qualität
            {
                return data.ToArray();
            }
        }
    }
    public static async Task<byte[]> TakeSelfieAsync()
    {
        try
        {
            // 1. Kamera-UI initialisieren
            CameraCaptureUI captureUI = new CameraCaptureUI();

            // 2. Einstellungen für Selfies optimieren
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            // Hinweis: Die Ausrichtung (Frontkamera) wird oft vom User im Dialog gewählt,
            // da CameraCaptureUI primär den System-Dialog triggert.

            // 3. Dialog öffnen
            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User hat den Vorgang abgebrochen
                return null;
            }

            // 4. In Byte-Array umwandeln
            using (var stream = await photo.OpenStreamForReadAsync())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error taking photo: {ex.Message}");
            return null;
        }
    }

}