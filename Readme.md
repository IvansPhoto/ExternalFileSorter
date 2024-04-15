# External file sorter

## FileGenerator
The project contains the app to generate a text file.

## FileSorter project
The project contains an app to sort a text file. There are two ways to split the source file for small files: default behaviour uses the default ArrayPoll with background processes and the second via channel and "runner".
The default processor with ArrayPool works faster but the approach with channel-runner is more stable in memory consumption.

## Benchmark Result
Benchmark works a little more slowly than running the app in the console.
Running the app from a terminal for a 10Gb file takes approximately 30 seconds with the ArrayPool approach.