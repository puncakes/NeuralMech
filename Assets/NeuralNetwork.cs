using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class NeuralNetwork
{
	public Guid Guid;
	public int Score { get; set; }

	public List<double> _weights;
	public double[] Weights;
	public List<int> WeightLayerIndexes;
	public List<IActivationFunction> WeightLayerActivationFunctions;
	
	public List<int> NeuronsInLayer;
	public List<int> BiasNeuronsInLayer;
	private double[] _output;

	public NeuralNetwork()
	{
		//x   x <---neuron layer (2 neurons)
		//| X | <---weight layer (4 weights)
		//x   x <---neuron layer (2 neurons)
		
		_weights = new List<double>();        //the array of weights between the neurons in each layer
		WeightLayerIndexes = new List<int>();//the array of indexes pointing at the start of the weights in a layer
		
		NeuronsInLayer = new List<int>();
		BiasNeuronsInLayer = new List<int>();
		WeightLayerActivationFunctions = new List<IActivationFunction>();
		_output = new double[1000];
	}

	public System.Object Clone()
	{
		NeuralNetwork result = (NeuralNetwork)ObjectCloner.DeepCopy(this);
		result.Guid = System.Guid.NewGuid();
		return result;
	}
	
	public void addLayer(IActivationFunction activationFunciton, int neuronCount, int biasCount)
	{
		NeuronsInLayer.Add(neuronCount);
		BiasNeuronsInLayer.Add(biasCount);
		WeightLayerActivationFunctions.Add(activationFunciton);
	}
	
	public void finalize()
	{
		int fromLayer = 0;
		int toLayer = NeuronsInLayer.Count - 1;
		
		for (int i = 0; i < toLayer; i++)
		{
			//starting at 0, add the weight index for each weight layer
			WeightLayerIndexes.Add(_weights.Count);
			
			for (int x = 0; x < NeuronsInLayer[fromLayer] + BiasNeuronsInLayer[fromLayer]; x++)
			{
				for (int y = 0; y < NeuronsInLayer[fromLayer + 1]; y++)
				{
					double d = 0;
					_weights.Add(d);
				}
			}
			fromLayer++;
		}
		Weights = _weights.ToArray();
		
	}

	public int CalculateSize()
	{
		return _weights.Count;
	}
	
	public double[] Compute(double[] input)
	{
		//do this next
		Array.Copy(input, _output, NeuronsInLayer[0]);
		
		for (int i = 0; i < WeightLayerIndexes.Count; i++)
		{
			ComputeLayer(i);
		}
		
		double[] output = new double[NeuronsInLayer[NeuronsInLayer.Count - 1]];
		Array.Copy(_output, output, output.Length);
		return output;
	}

	public void Randomize()
	{
		for (int i = 0; i < Weights.Length; i++)
		{
			//randomize weights from -1 to 1
			Weights[i] = (ThreadSafeRandom.NextDouble() * 2.0) - 1.0;
		}
	}
	
	private void ComputeLayer(int currentLayer)
	{
		int inputNeuronCount = NeuronsInLayer[currentLayer];
		int biasNeuronCount = BiasNeuronsInLayer[currentLayer];
		int outputNeuronCount = NeuronsInLayer[currentLayer + 1];            
		int weightIndexOffset = WeightLayerIndexes[currentLayer];
		double[] output = new double[outputNeuronCount];
		
		for (int i = 0; i < outputNeuronCount; i++)
		{
			double sum = 0;
			for (int j = 0; j < inputNeuronCount; j++)
			{
				sum += Weights[weightIndexOffset++] * _output[j];
			}
			for (int j = 0; j < biasNeuronCount; j++)
			{
				sum += Weights[weightIndexOffset++];
			}
			output[i] = sum;
		}
		WeightLayerActivationFunctions[currentLayer].ActivationFunction(output, 0, outputNeuronCount);
		for (int i = 0; i < outputNeuronCount; i++)
		{
			_output[i] = output[i];
		}
	}
}

