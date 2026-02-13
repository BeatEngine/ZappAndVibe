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
        using (var data = SKData.CreateCopy(imageData))
        using (var bitmap = SKBitmap.Decode(data))
        {
            if (bitmap == null)
            {
                Console.WriteLine("Error no picture");
                return null;
            }    

            var info = new SKImageInfo(width, height);
            using (var resized = bitmap.Resize(info, SKFilterQuality.High))
            {
                if (resized == null)
                {
                    Console.WriteLine("Error resizing picture");
                    return null;
                }
                using (var image = SKImage.FromBitmap(resized))
                    using (var encodedData = image.Encode(SKEncodedImageFormat.Jpeg, 80))
                    {
                        return encodedData.ToArray();
                    }
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