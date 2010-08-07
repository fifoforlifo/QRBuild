#include <stdio.h>
#define GENERATED_HEADER_NAME "generated.h"
#include GENERATED_HEADER_NAME

int main(int argc, char** argv)
{
	printf("%s %s %s\n", a(), b(), c());
	return 0;
}
