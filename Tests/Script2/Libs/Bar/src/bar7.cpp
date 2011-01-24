#include "bar1.h"
#include "bar0.h"

int bar7(Bar *pBar)
{
	if (IsDebuggerPresent())
	{
		return 30;
	}
	return bar0(pBar) + bar1(pBar) + 2;
}

int bar7b(Bar *pBar)
{
	if (IsDebuggerPresent())
	{
		return 49;
	}
	return bar0(pBar) + bar1(pBar) + 2;
}
