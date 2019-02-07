using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Hmmm
	We could first do a 3 dimensional LCE, but I want to do more.
	For visualization though, I then need some projection to 3dim or below.
	I guess I could use T-SNE or auto-encoding for that.
	With something as rich as audio, I doubt 3D will show me very much.

	Edit: oh, this is fun!
 */

public class LaggedCoordinateEmbedding : MonoBehaviour {
	[SerializeField] private AudioClip _clip;
	
	[SerializeField] private int _numTrajectories = 32;
    [SerializeField] private int _pointsPerTrajectory = 4;

    [SerializeField] private float _scale = 20f;
	[SerializeField] private int _skipSeconds = 1;
    [SerializeField] private int _samplesPerRenderFrame = 1;
    [SerializeField] private int _pointsStride = 3;
	[SerializeField] private int _basisStride = 1;

	float[] _data;
	Vector3[] _trajectories;

	int _bufferIdx;
	int _ringIdx;

	void Start () {
		Application.targetFrameRate = 60;
		
		_data = new float[_clip.samples];
		_clip.GetData(_data, 0);

        _trajectories = new Vector3[_numTrajectories * _pointsPerTrajectory];

        _bufferIdx = _clip.frequency* _skipSeconds;
	}

	private void Update() {
		int dims = 3;
        _ringIdx = (_ringIdx + 1) % _pointsPerTrajectory;

        Debug.Log("" + _bufferIdx / (float)_clip.samples * 100f + "%");

        for (int i = 0; i < _numTrajectories; i++) {
			Vector3 samplePos = Vector3.zero;

			int idx = _bufferIdx + _pointsStride * i;
			if(idx < _data.Length - dims * _basisStride) {
                samplePos = new Vector3(_data[idx + 0 * _basisStride], _data[idx + 1 * _basisStride], _data[idx + 2 * _basisStride]) * _scale;
			}
            
			_trajectories[i * _pointsPerTrajectory + _ringIdx] = samplePos;
        }

		_bufferIdx += _samplesPerRenderFrame;
	}
	
	void OnDrawGizmos() {
		if (!Application.isPlaying) {
			return;
		}

		Random.InitState(1234);

        for (int i = 0; i < _numTrajectories; i++) {
            Color lineColor = Color.HSVToRGB(Random.value, 0.5f, 0.7f);
			lineColor.a = 0.4f;
			Gizmos.color = lineColor;

			int trajectoryIdx = i * _pointsPerTrajectory;

			for (int j = 1; j < _pointsPerTrajectory; j++) {
				Gizmos.DrawLine(
					_trajectories[trajectoryIdx + (_ringIdx + j) % _pointsPerTrajectory],
					_trajectories[trajectoryIdx + (_ringIdx + j + 1) % _pointsPerTrajectory]);
			}

            Gizmos.DrawSphere(_trajectories[trajectoryIdx + _ringIdx], 0.1f);
        }
	}
}
