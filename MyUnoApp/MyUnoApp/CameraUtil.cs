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
        using (var codec = SKCodec.Create(data))
        using (var bitmap = SKBitmap.Decode(codec))
        {
            if (bitmap == null) return null;

            // Create a surface for the target size
            var info = new SKImageInfo(width, height);
            using (var surface = SKSurface.Create(info))
            using (var canvas = surface.Canvas)
            {
                canvas.Clear(SKColors.Transparent);

                // Get the orientation from the image metadata
                var orientation = codec.EncodedOrigin;
                var matrix = SKMatrix.CreateIdentity();

                // Apply transformations based on the EXIF tag
                switch (orientation)
                {
                    case SKEncodedOrigin.TopRight: // Mirrored horizontal
                        canvas.Scale(-1, 1, width / 2.0f, height / 2.0f);
                        break;
                    case SKEncodedOrigin.BottomRight: // Rotate 180
                        canvas.RotateDegrees(180, width / 2.0f, height / 2.0f);
                        break;
                    case SKEncodedOrigin.BottomLeft: // Mirrored vertical
                        canvas.Scale(1, -1, width / 2.0f, height / 2.0f);
                        break;
                    case SKEncodedOrigin.LeftTop: // Mirrored horizontal & rotate 270
                        canvas.RotateDegrees(270, width / 2.0f, height / 2.0f);
                        canvas.Scale(-1, 1, width / 2.0f, height / 2.0f);
                        break;
                    case SKEncodedOrigin.RightTop: // Rotate 90 (Your specific issue)
                        canvas.RotateDegrees(90, width / 2.0f, height / 2.0f);
                        break;
                    case SKEncodedOrigin.RightBottom: // Mirrored horizontal & rotate 90
                        canvas.RotateDegrees(90, width / 2.0f, height / 2.0f);
                        canvas.Scale(-1, 1, width / 2.0f, height / 2.0f);
                        break;
                    case SKEncodedOrigin.LeftBottom: // Rotate 270
                        canvas.RotateDegrees(270, width / 2.0f, height / 2.0f);
                        break;
                    default: // Default / TopLeft (Correct orientation)
                        break;
                }

                // Draw the original bitmap into the corrected canvas
                var destRect = new SKRect(0, 0, width, height);
                canvas.DrawBitmap(bitmap, destRect);

                using (var image = surface.Snapshot())
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
