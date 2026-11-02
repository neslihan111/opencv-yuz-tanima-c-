using OpenCvSharp;

namespace FaceDetection;

/// <summary>
/// Haar cascade sınıflandırıcısı ile kareler içindeki yüzleri bulur.
/// </summary>
public sealed class FaceDetector : IDisposable
{
    private readonly CascadeClassifier _classifier;
    private bool _disposed;

    public FaceDetector(string cascadePath)
    {
        if (!File.Exists(cascadePath))
        {
            throw new FileNotFoundException("Haar cascade dosyası bulunamadı.", cascadePath);
        }

        _classifier = new CascadeClassifier(cascadePath);
    }

    /// <summary>
    /// Verilen karede yüzleri algılar. Kare gri tonlamaya çevrilir ve histogram eşitlenir.
    /// </summary>
    public Rect[] DetectFaces(Mat frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var gray = new Mat();
        if (frame.Channels() == 1)
        {
            frame.CopyTo(gray);
        }
        else if (frame.Channels() == 4)
        {
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        }

        Cv2.EqualizeHist(gray, gray);

        return _classifier.DetectMultiScale(
            gray,
            1.1,
            5,
            HaarDetectionTypes.ScaleImage,
            new OpenCvSharp.Size(30, 30));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _classifier.Dispose();
        _disposed = true;
    }
}
