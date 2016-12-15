# building `LMDB` on windows

Following necessary GNU headers added to build lib as explain [here](https://initialneil.wordpress.com/2015/01/11/build-caffe-in-windows-with-visual-studio-2013-cuda-6-5-opencv-2-4-9/);

`unistd.h`, `getopt.h` and `getopt.c`



### usage
Use `premake5` script at path `lmdb/libraries` with same folder of source files `liblmdb` to generate `VS2015` project;

```premake5 vs2015```


set your `ENV` variables to direct LMDB paths `release/include` and `release/lib`.
