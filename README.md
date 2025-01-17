# progaudi.tarantool

Dotnet client for Tarantool NoSql database.
Fork from [progaudi.tarantool](https://www.nuget.org/packages/progaudi.tarantool).

# Key features
 - Full [IProto](https://tarantool.org/doc/dev_guide/box-protocol.html) protocol coverage.
 - Async API.
 - Multiplexing.

# Installation

Simpliest way to start using ```tarantool-dotnet``` in your project is to install it from [Nuget](https://www.nuget.org/packages/Tarantool.DotNet/).

# Demo

[We have a small demo](https://github.com/progaudi/progaudi.tarantool/blob/master/samples/docker-compose/). It illustrates usage of library in aspnet core with docker-compose. Docker 1.12+ is preferred.

# Usage

You can find basic usage scenarios in [index](https://github.com/progaudi/progaudi.tarantool/blob/master/tests/progaudi.tarantool.tests/Index/Smoke.cs) and [space](https://github.com/progaudi/progaudi.tarantool/blob/master/tests/progaudi.tarantool.tests/Space/Smoke.cs) smoke tests.

# Limitations

We were trying to make API similar with tarantool lua API. So this connector is straightforward implementing of [IProto protocol](https://tarantool.org/doc/dev_guide/internals_index.html). Some methods are not implemented yet because there are no direct analogs in IProto. Implementing some methods (like Pairs) does not make any sense, because it return a lua-iterator.

When API will be finalized all methods would be implemented or removed from public API.

* Index methods:
    1. Pairs
    2. Count
    3. Alter
    4. Drop
    5. Rename
    6. BSize
    7. Alter
* Schema methods
    1. CreateSpace
* Space methods
    1. CreateIndex
    2. Drop
    3. Rename
    4. Count
    5. Lengh
    6. Increment
    7. Decrement
    8. AutoIncrement
    9. Pairs
