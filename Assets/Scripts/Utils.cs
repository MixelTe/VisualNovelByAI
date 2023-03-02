using UnityEngine;

static public class Utils
{
	static public Rect Inflate(this Rect rect, int v)
	{
		return new Rect(rect.x + v, rect.y + v, rect.width + v, rect.height + v);
	}
}
