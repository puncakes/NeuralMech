/// <summary>
/// A simple Object cloner that uses serialization. Actually works really well
/// for the somewhat complex nature of BasicNetwork. Performs a deep copy without
/// all the headache of programming a custom clone.
/// 
/// From a Java example at:
/// 
/// http://www.javaworld.com/javaworld/javatips/jw-javatip76.html?page=2
/// 
/// </summary>
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class ObjectCloner
{
	/// <summary>
	/// Private constructor.
	/// </summary>
	private ObjectCloner()
	{
	}
	
	/// <summary>
	/// Perform a deep copy.
	/// </summary>
	/// <param name="oldObj">The old object.</param>
	/// <returns>The new object.</returns>
	public static Object DeepCopy(Object oldObj)
	{
		var formatter = new BinaryFormatter();
		
		using (var memory = new MemoryStream())
		{
			try
			{
				// serialize and pass the object
				formatter.Serialize(memory, oldObj);
				memory.Flush();
				memory.Position = 0;
				
				// return the new object
				return formatter.Deserialize(memory);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}
		return null;
	}
}