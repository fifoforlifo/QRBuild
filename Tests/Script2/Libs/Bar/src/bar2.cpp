#include "bar1.h"
#include "bar0.h"

int bar2(Bar *pBar)
{
	return bar0(pBar) + bar1(pBar) + 2;
}
