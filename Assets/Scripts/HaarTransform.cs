using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Rng = Unity.Mathematics.Random;

/*
    Implementation Notes

    - Slice notation is very welcome! Writing the recursive logic
    for the multi-resolution Haar transform becomes very simple.
    This is how Rust does it, except that Run might have additional
    tricks available?
    - Going through floating-point paths to calculate logarithms that
    we know will range into the natural numbers is weird.
    - In Rust, instead of writing our Transform functions with type
    signature Slice<float>, we can write: Slice<TNum>, where TNum
    is any type that supports addition, subtraction, and division-by-sqrt(2)
        - Now, dividing by an irrational isn't great for fixed point, but TNum
        could totally be a ring of fixed point types. That sqrt(2) though...

 */

public class HaarTransform : MonoBehaviour {
    private NativeArray<float> _input;

    private NativeArray<float> _bufferA;
    private NativeArray<float> _bufferB;

    private const float sqrt2 = 1.41421356237f;

    private void Awake() {
        const int signalLength = 64;
        _input = new NativeArray<float>(signalLength, Allocator.Persistent);
        _bufferA = new NativeArray<float>(signalLength, Allocator.Persistent);
        _bufferB = new NativeArray<float>(signalLength, Allocator.Persistent);

        int maxDepth = MaxTransformDepth(signalLength);
        Debug.LogFormat("Signal length: {0}, Max depth: {1}", signalLength, maxDepth);

        Generate(_input);
        Copy(_input, _bufferA);
        var output = Transform(_bufferA, _bufferB, maxDepth);

        double eInput = SumOfSqrs(_input);
        double eOutput = SumOfSqrs(output);
        Debug.LogFormat("Energy difference: {0}", eInput - eOutput);
    }

    private void OnDestroy() {
        _input.Dispose();
        _bufferA.Dispose();
        _bufferB.Dispose();
    }

    private static void Generate(NativeArray<float> input) {
        const double freq = 10.0;
        float phaseStep = (float)(math.PI * 2.0 / (double)(input.Length-1) * freq);
        for (int i = 0; i < input.Length; i++) {
            input[i] = math.sin(phaseStep * i);
        }
    }

    private static void Copy(NativeArray<float> a, NativeArray<float> b) {
        for (int i = 0; i < a.Length; i++) {
            b[i] = a[i];
        }
    }

    /// <summary>
    /// Recursively applies Haar transform up to given depth of iterations.
    /// </summary>
    /// <param name="a">Buffer containing input data</param>
    /// <param name="b">Additional buffer used in computation</param>
    /// <param name="depth">Pointer to buffer containing output data, which is either a or b.</param>
    /// <returns></returns>
    private static NativeSlice<float> Transform(NativeSlice<float> a, NativeSlice<float> b, int depth) {
        if (depth > MaxTransformDepth(a.Length)) {
            throw new System.ArgumentException(string.Format("Depth {0} cannot exceed maximum depth of {1} for input with length {2}", depth, MaxTransformDepth(a.Length), a.Length));
        }

        for (int i = 0; i < depth; i++) {
            // transform on slices recursively, pingponging between the arrays
            // each level transforms the previous trend signal, which is the
            // left half of the input buffer
            int extents = a.Length / ((int)Mathf.Pow(2, i));
            Transform(a.Slice(0, extents), b.Slice(0, extents));

            // swap the buffers
            var temp = a;
            a = b;
            b = temp;
        }

        return a;
    }

    /// <summary>
    /// Applies one level of the Haar transform
    /// </summary>
    /// <param name="input">Input buffer</param>
    /// <param name="output">Output buffer containing [trend,fluctuations]</param>
    private static void Transform(NativeSlice<float> input, NativeSlice<float> output) {
        var trend = output.Slice(0, input.Length/2);
        var fluct = output.Slice(input.Length/2, input.Length/2);

        for (int i = 0; i < trend.Length; i++) {
            trend[i] = (input[i*2] + input[i*2+1]) / sqrt2;
            fluct[i] = (input[i*2] - input[i*2+1]) / sqrt2;
        }
    }

    private static double SumOfSqrs(NativeSlice<float> signal) {
        double e = 0.0;
        for (int i = 0; i < signal.Length; i++) {
            e += signal[i] * signal[i];
        }
        return e;
    }

    private static int MaxTransformDepth(int signalLength) {
        return (int)math.round(math.log2(signalLength)) - 1;
    }
}