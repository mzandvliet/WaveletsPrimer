using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Rng = Unity.Mathematics.Random;

public class HaarTransform : MonoBehaviour {
    private NativeArray<float> _input;
    private NativeArray<float> _output;

    private const float sqrt2 = 1.41421356237f;

    private void Awake() {
        _input = new NativeArray<float>(64, Allocator.Persistent);
        _output = new NativeArray<float>(64, Allocator.Persistent);

        Generate(_input);
        Transform(_input, _output);

        double eInput = Energy(_input);
        double eOutput = Energy(_output);
        Debug.LogFormat("Energy difference: {0}", eInput - eOutput);
    }

    private void OnDestroy() {
        _input.Dispose();
        _output.Dispose();
    }

    private static void Generate(NativeArray<float> input) {
        const double freq = 10.0;
        float phaseStep = (float)(math.PI * 2.0 / (double)(input.Length-1) * freq);
        for (int i = 0; i < input.Length; i++) {
            input[i] = math.sin(phaseStep * i);
        }
    }


    private static void Transform(NativeArray<float> input, NativeArray<float> output) {
        var trend = output.Slice(0, input.Length/2);
        var fluct = output.Slice(input.Length/2, input.Length/2);

        for (int i = 0; i < trend.Length; i++) {
            trend[i] = (input[i*2] + input[i*2+1]) / sqrt2;
            fluct[i] = (input[i*2] - input[i*2+1]) / sqrt2;
        }
    }

    private static double Energy(NativeArray<float> signal) {
        double e = 0.0;
        for (int i = 0; i < signal.Length; i++) {
            e += signal[i] * signal[i];
        }
        return e;
    }
}