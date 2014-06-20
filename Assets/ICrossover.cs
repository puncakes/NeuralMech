using UnityEngine;
using System.Collections.Generic;

public interface ICrossover
{
	void Mate(Robot mother, Robot father,
	          Robot offspring1, Robot offspring2);
}


