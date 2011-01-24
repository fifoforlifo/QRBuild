#include "bar1.h"
#include "bar0.h"
#include <set>

int bar5(Bar *pBar)
{
	if (IsDebuggerPresent())
	{
		return 13;
	}
	return bar0(pBar) + bar1(pBar) + 2;
}
