using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveProcessor : MonoBehaviour
{
	public struct wave
	{
		public Vector3 pos;
		public float phase;
	}

	public ComputeShader mediumCompute = null;
	int kiWavesUpdate, kiSurfersUpdate;
	Oscillator[] oscillatorsList;
	Surfer[] surfersList;
	ComputeBuffer wavesBuffer, wavesUpdateBuffer, surfersBuffer;
	wave[] wavesArray;
	Vector3[] surfersArray;


	public float waveSpeed = 1;
	public float framePeriod = 0.1f;

	public int wavesCount = 256;
	int curWaveIndex = 0;

	void Start()
	{
		oscillatorsList = gameObject.GetComponentsInChildren<Oscillator>();
		wavesArray = new wave[oscillatorsList.Length * wavesCount];
		for (int i = 0; i < oscillatorsList.Length; i++){
			Oscillator osc = oscillatorsList[i];
			osc.phase += osc.frequency * framePeriod;
			for (int j = 0; j < wavesCount; j++){
				int wi = i * wavesCount + j;
				wavesArray[wi].pos = osc.transform.position;
				wavesArray[wi].phase = 0;
			}
		}
		wavesBuffer = new ComputeBuffer(wavesArray.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(wave)), ComputeBufferType.Default);
		wavesBuffer.SetData(wavesArray);
		wavesArray = new wave[oscillatorsList.Length];
		wavesUpdateBuffer = new ComputeBuffer(wavesArray.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(wave)), ComputeBufferType.Default);
		
		kiWavesUpdate = mediumCompute.FindKernel("wavesUpdate");
		mediumCompute.SetBuffer(kiWavesUpdate, "wavesBuffer", wavesBuffer);
		mediumCompute.SetBuffer(kiWavesUpdate, "wavesUpdateBuffer", wavesUpdateBuffer);

		surfersList = gameObject.GetComponentsInChildren<Surfer>();
		if (surfersList.Length > 0)
		{
			surfersArray = new Vector3[surfersList.Length];
			for (int i = 0; i < surfersList.Length; i++){
				surfersArray[i] = surfersList[i].transform.position;
			}
			surfersBuffer = new ComputeBuffer(surfersArray.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
			surfersBuffer.SetData(surfersArray);

			kiSurfersUpdate = mediumCompute.FindKernel("surfersUpdate");
			mediumCompute.SetBuffer(kiSurfersUpdate, "wavesBuffer", wavesBuffer);
			mediumCompute.SetBuffer(kiSurfersUpdate, "surfersBuffer", surfersBuffer);
		}

		mediumCompute.SetFloat("waveSpeed", waveSpeed);
		mediumCompute.SetFloat("framePeriod", framePeriod);
		mediumCompute.SetInt("oscillatorsCount", oscillatorsList.Length);
		mediumCompute.SetInt("wavesCount", wavesCount);
		mediumCompute.SetInt("curWaveIndex", curWaveIndex);
		
		for (int i = 0; i < wavesCount; i++)
			wavesUpdate();

		Shader.SetGlobalBuffer("wavesBuffer", wavesBuffer);
		Shader.SetGlobalFloat("waveSpeed", waveSpeed);
		Shader.SetGlobalFloat("framePeriod", framePeriod);
		Shader.SetGlobalInt("oscillatorsCount", oscillatorsList.Length);
		Shader.SetGlobalInt("wavesCount", wavesCount);
		Shader.SetGlobalInt("curWaveIndex", curWaveIndex);
	}

	void surfersUpdate()
	{
		if (surfersList.Length < 1)
			return;

		for (int i = 0; i < surfersList.Length; i++)
			surfersArray[i] = surfersList[i].transform.position;

		surfersBuffer.SetData(surfersArray);
		mediumCompute.Dispatch(kiSurfersUpdate, surfersList.Length, 1, 1);
		surfersBuffer.GetData(surfersArray);
		
		for (int i = 0; i < surfersList.Length; i++)
		{
			//Debug.Log(surfersArray[i]);
			surfersList[i].transform.position = surfersArray[i];
		}
		//Debug.Log("qwe");
	}

	void wavesUpdate()
	{
		for (int i = 0; i < oscillatorsList.Length; i++)
		{
			Oscillator osc = oscillatorsList[i];
			osc.phase += osc.frequency * framePeriod;
			wavesArray[i].pos = osc.transform.position;
			wavesArray[i].phase = osc.phase;
		}
		wavesUpdateBuffer.SetData(wavesArray);
		mediumCompute.Dispatch(kiWavesUpdate, oscillatorsList.Length, 1, 1);
		
		curWaveIndex = (curWaveIndex + 1) % wavesCount;
		Shader.SetGlobalInt("curWaveIndex", curWaveIndex);
		mediumCompute.SetInt("curWaveIndex", curWaveIndex);
	}

	void Update()
	{
		surfersUpdate();
		wavesUpdate();


	}

	void OnDestroy()
    {
        wavesBuffer.Release();
        wavesUpdateBuffer.Release();
		surfersBuffer.Release();
    }
}
