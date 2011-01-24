#include "bar1.h"
#include "bar0.h"

int bar6(Bar *pBar)
{
	if (sizeof(void*) == 8)
	{
		return 14;
	}
	return bar0(pBar) + bar1(pBar) + 2;
}
