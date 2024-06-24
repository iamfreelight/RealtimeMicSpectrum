using UnityEngine;

public class RealtimeMicSpectrum : MonoBehaviour
{
    public int sampleRate = 22050;
    public int spectrumSize = 5000; // Size of the FFT array, must be a power of 2
    public int numberOfBars = 64; // Number of bars to display
    public float sensitivity = 50f; // Sensitivity of the visualization

    private AudioClip microphoneClip;
    private string microphoneDevice;
    private bool isMicrophoneStarted = false;

    private float[] spectrumData;
    private float[] samples;

    void Start()
    {
        StartMicrophone();
    }

    void Update()
    {
        if (isMicrophoneStarted)
        {
            AnalyzeSound();
        }
    }

    void StartMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            microphoneClip = Microphone.Start(microphoneDevice, true, 1, sampleRate);
            while (!(Microphone.GetPosition(microphoneDevice) > 0)) { }
            isMicrophoneStarted = true;
            Debug.Log("Microphone started: " + microphoneDevice);
        }
        else
        {
            Debug.LogWarning("No microphone devices found.");
        }
    }

    void AnalyzeSound()
    {
        if (microphoneClip == null)
        {
            Debug.LogWarning("Microphone clip is null.");
            return;
        }

        int micPosition = Microphone.GetPosition(microphoneDevice) - spectrumSize;
        if (micPosition < 0)
        {
            return;
        }

        samples = new float[spectrumSize];
        microphoneClip.GetData(samples, micPosition);

        // Apply Blackman window to samples
        ApplyBlackmanWindow(samples);

        // Compute spectrum data from samples using FFT
        spectrumData = new float[spectrumSize];
        FFT(samples, ref spectrumData);
    }

    void ApplyBlackmanWindow(float[] samples)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            float blackman = 0.42f - 0.5f * Mathf.Cos(2 * Mathf.PI * i / (samples.Length - 1)) + 0.08f * Mathf.Cos(4 * Mathf.PI * i / (samples.Length - 1));
            samples[i] *= blackman;
        }
    }

    void FFT(float[] input, ref float[] output)
    {
        int N = input.Length;
        float[] real = new float[N];
        float[] imag = new float[N];

        // Perform FFT (Cooley-Tukey radix-2)
        for (int i = 0; i < N; i++)
        {
            real[i] = input[i];
            imag[i] = 0.0f;
        }

        FFTRecursive(real, imag, false);

        for (int i = 0; i < N; i++)
        {
            output[i] = Mathf.Sqrt(real[i] * real[i] + imag[i] * imag[i]) / N;
        }
    }

    void FFTRecursive(float[] real, float[] imag, bool invert)
    {
        int N = real.Length;
        if (N == 1) return;

        float[] evenReal = new float[N / 2];
        float[] evenImag = new float[N / 2];
        float[] oddReal = new float[N / 2];
        float[] oddImag = new float[N / 2];

        // Split input into even and odd indexed parts
        for (int i = 0; i < N / 2; i++)
        {
            evenReal[i] = real[2 * i];
            evenImag[i] = imag[2 * i];
            oddReal[i] = real[2 * i + 1];
            oddImag[i] = imag[2 * i + 1];
        }

        // Recursively compute FFT on even and odd parts
        FFTRecursive(evenReal, evenImag, invert);
        FFTRecursive(oddReal, oddImag, invert);

        // Combine results of even and odd parts
        float angle = 2 * Mathf.PI / N * (invert ? 1 : -1);
        float wReal = 1.0f;
        float wImag = 0.0f;

        for (int i = 0; i < N / 2; i++)
        {
            float currReal = wReal * oddReal[i] - wImag * oddImag[i];
            float currImag = wReal * oddImag[i] + wImag * oddReal[i];

            real[i] = evenReal[i] + currReal;
            imag[i] = evenImag[i] + currImag;
            real[i + N / 2] = evenReal[i] - currReal;
            imag[i + N / 2] = evenImag[i] - currImag;

            // Apply normalization if inverting FFT
            if (invert)
            {
                real[i] /= 2;
                imag[i] /= 2;
                real[i + N / 2] /= 2;
                imag[i + N / 2] /= 2;
            }

            // Update rotation factor (twiddle factor)
            float temp = wReal;
            wReal = Mathf.Cos(angle) * wReal - Mathf.Sin(angle) * wImag;
            wImag = Mathf.Sin(angle) * temp + Mathf.Cos(angle) * wImag;
        }
    }


    void OnGUI()
    {
        if (spectrumData == null || spectrumData.Length == 0)
        {
            return;
        }

        float barWidth = Screen.width / (float)numberOfBars;
        for (int i = 0; i < numberOfBars; i++)
        {
            float height = Mathf.Clamp(spectrumData[i] * sensitivity * Screen.height, 0, Screen.height);
            Rect barRect = new Rect(i * barWidth, Screen.height - height, barWidth, height);
            GUI.Box(barRect, GUIContent.none, GUI.skin.box);
        }
    }

    void OnDestroy()
    {
        StopMicrophone();
    }

    void OnDisable()
    {
        StopMicrophone();
    }

    void StopMicrophone()
    {
        if (isMicrophoneStarted)
        {
            Microphone.End(microphoneDevice);
            isMicrophoneStarted = false;
            Debug.Log("Microphone stopped.");
        }
    }
}
