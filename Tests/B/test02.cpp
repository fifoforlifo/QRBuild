#include <stdio.h>
//#include "jank.h"

int aoo();
int boo();
int coo();
int doo();
int foo();
int groo();
int qoo();
int yoo();

int main()
{
    int x = 
			aoo()
		|	boo()
		|	coo()
		|	doo()
		|	foo()
		|	groo()
		|	qoo()
		|	yoo()
		;
	printf("test02 foo()=%d\n", x);
    return 0;
}
