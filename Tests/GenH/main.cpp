#include <stdio.h>
#include "generated.h"
#include "boost/program_options.hpp"
#include "boost/format.hpp"
#include "boost/functional.hpp"
#include "boost/shared_ptr.hpp"
#include "boost/regex.hpp"

int main(int argc, char** argv)
{
	printf("%s %s %s\n", a(), b(), c());
	return 0;
}
