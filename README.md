# LMDB for Visual Studio

To compile just compile.

**lmdb.props** has next settings by default:

* Install target installs binaries and headers to **..\\..\\msvc\\$(Platform)**.
* Debug binaries have **d** suffix.

You can try to override them with **/p** msbuild option or via property manager.