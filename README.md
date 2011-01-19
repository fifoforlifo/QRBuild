# QRBuild

QRBuild is a strongly-typed build system with automatic file-level dependency
checking.  Additionally, it supports an evolving project-system where project
"scripts" are entirely written in C#.

QRBuild itself is implemented in C# and targets Windows OSes.

*At this time, QRBuild is just a prototype.*

### Goals

Some of the goals of this project are:  

* Demonstrate that a strongly-typed build system could work.  
* Achieve high performance builds on large numbers of files and dependencies.  
* Minimize the amount of redundant information specified by authors
  of build scripts.  For example, dependencies should be automatically
  determined, rather than explicitly enumerated.  
* Take advantage of the C# environment.  
  * good editor support (code-browsing, etc.)  
  * ability to define functions, local variables, etc.  
* Develop a set of good practices and patterns in the design of a  
  build system, build scripts, and source-organization.  
