using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wavelet : MonoBehaviour {
    [SerializeField] private Texture2D _input;

    private Texture2D _output;

    void Start () {
        _output = new Texture2D(_input.width, _input.height, TextureFormat.ARGB32, false);
        Color[] colors = _input.GetPixels(0, 0, _input.width, _input.height);
        var grayscale = ToFloats(colors);

        const int iterations = 1;
        FWT(grayscale, iterations);
        // IWT(grayscale, iterations);

        colors = ToColors(grayscale);
        _output.SetPixels(colors);
        _output.Apply();
    }

    private float[,] ToFloats(Color[] img) {
        var pix = new float[_input.width, _input.height];
        for (int x = 0; x < _input.width; x++) {
            for (int y = 0; y < _input.height; y++) {
                pix[x, y] = img[x * _input.width + y].grayscale;
            }
        }
        return pix;
    }

    private static Color[] ToColors(float[,] img) {
        int w = img.GetLength(0);
        int h = img.GetLength(1);

        Color[] cols = new Color[w * h];
        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                cols[x * w + y] = new Color(img[x, y], img[x, y], img[x, y], 1f);
            }
        }
        return cols;
    }

    public static void FWT(float[,] data, int iterations) {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        for (int k = 0; k < iterations; k++) {
            int lev = 1 << k;

            int levCols = cols / lev;
            int levRows = rows / lev;

            var row = new float[levCols];
            for (int x = 0; x < levRows; x++) {
                for (int y = 0; y < row.Length; y++)
                    row[y] = data[x, y];

                FWT(row);

                for (int y = 0; y < row.Length; y++)
                    data[x, y] = row[y];
            }


            var col = new float[levRows];
            for (int y = 0; y < levCols; y++) {
                for (int x = 0; x < col.Length; x++)
                    col[x] = data[x, y];

                FWT(col);

                for (int x = 0; x < col.Length; x++)
                    data[x, y] = col[x];
            }
        }
    }

    public static void FWT(float[] data) {
        var temp = new float[data.Length];

        const float s0 = 0.5f;
        const float s1 = 0.5f;
        const float w0 = 0.5f;
        const float w1 = -0.5f;

        int h = data.Length >> 1;
        for (int i = 0; i < h; i++) {
            int k = (i << 1);

            // For the regular forward transform
            // temp[i] = data[k] * s0 + data[k + 1] * s1;
            // temp[i + h] = data[k] * w0 + data[k + 1] * w1;

            // For visualizing the coefs (note: don't do an inverse transform on this result)
           temp[i] = (data[k] + data[k + 1]) * 0.5f;
           temp[i + h] = 0.5f + 0.5f * (data[k] - data[k + 1]);
        }

        for (int i = 0; i < data.Length; i++)
            data[i] = temp[i];
    }

    public void IWT(float[,] data, int iterations) {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        for (int k = iterations - 1; k >= 0; k--) {
            int lev = 1 << k;

            int levCols = cols / lev;
            int levRows = rows / lev;

            var col = new float[levRows];
            for (int y = 0; y < levCols; y++) {
                for (int x = 0; x < col.Length; x++)
                    col[x] = data[x, y];

                IWT(col);

                for (int x = 0; x < col.Length; x++)
                    data[x, y] = col[x];
            }

            var row = new float[levCols];
            for (int x = 0; x < levRows; x++) {
                for (int y = 0; y < row.Length; y++)
                    row[y] = data[x, y];

                IWT(row);

                for (int y = 0; y < row.Length; y++)
                    data[x, y] = row[y];
            }
        }
    }

    public static void IWT(float[] data) {
        var temp = new float[data.Length];

        const float s0 = 0.5f;
        const float s1 = 0.5f;
        const float w0 = 0.5f;
        const float w1 = -0.5f;

        int h = data.Length >> 1;
        for (int i = 0; i < h; i++) {
            int k = (i << 1);
            temp[k] = (data[i] * s0 + data[i + h] * w0) / w0;
            temp[k + 1] = (data[i] * s1 + data[i + h] * w1) / s0;
        }

        for (int i = 0; i < data.Length; i++)
            data[i] = temp[i];
    }

    private void OnGUI() {
        GUI.DrawTexture(new Rect(0,0,_output.width,_output.height), _output, ScaleMode.ScaleToFit);
    }
}
