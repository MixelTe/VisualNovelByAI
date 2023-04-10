using UnityEngine;

static public class Utils
{
	static public Rect Inflate(this Rect rect, float v)
	{
		return new Rect(rect.x + v, rect.y + v, rect.width + v, rect.height + v);
	}
	static public Rect Mul(this Rect rect, float v)
	{
		return new Rect(rect.x * v, rect.y * v, rect.width * v, rect.height * v);
	}
}
