using OpenCvSharp;

namespace FaceDetection;

/// <summary>
/// Webcam'den kare okuyan servis. Okuma işi arka plan thread'inde yapılır;
/// en son kare kilit (lock) ile korunur ve kopyası dışarıya verilir.
/// </summary>
public sealed class WebcamService : IDisposable
{
    private readonly object _frameLock = new();
    private VideoCapture? _capture;
    private Thread? _captureThread;
    private Mat? _latestFrame;
    private volatile bool _isRunning;
    private bool _disposed;

    public bool IsRunning => _isRunning;

    public void Start(int cameraIndex = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isRunning)
        {
            return;
        }

        var capture = new VideoCapture(cameraIndex);
        if (!capture.IsOpened())
        {
            capture.Dispose();
            throw new InvalidOperationException($"{cameraIndex} numaralı kamera açılamadı.");
        }

        _capture = capture;
        _isRunning = true;
        _captureThread = new Thread(CaptureLoop) { IsBackground = true, Name = "WebcamCaptureLoop" };
        _captureThread.Start();
    }

    public void Stop()
    {
        if (!_isRunning && _capture is null)
        {
            return;
        }

        _isRunning = false;

        // Thread bitmeden kamerayı kapatmamak için önce bekliyoruz.
        _captureThread?.Join(2000);
        _captureThread = null;

        _capture?.Dispose();
        _capture = null;

        lock (_frameLock)
        {
            _latestFrame?.Dispose();
            _latestFrame = null;
        }
    }

    /// <summary>
    /// En son kareyi kopya olarak döndürür. Çağıran taraf döndürülen Mat'i dispose etmelidir.
    /// Henüz kare yoksa null döner.
    /// </summary>
    public Mat? GetLatestFrame()
    {
        lock (_frameLock)
        {
            return _latestFrame?.Clone();
        }
    }

    private void CaptureLoop()
    {
        VideoCapture? capture = _capture;
        if (capture is null)
        {
            return;
        }

        using var frame = new Mat();
        while (_isRunning)
        {
            if (!capture.Read(frame) || frame.Empty())
            {
                Thread.Sleep(10);
                continue;
            }

            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = frame.Clone();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}
