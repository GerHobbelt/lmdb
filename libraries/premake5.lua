workspace "lmdb"
  configurations { "Debug", "Release" }
  platforms({"x64"})
  location("build/" .. _ACTION)
  startproject "liblmdb"
  targetdir "./bin/%{_ACTION}/%{cfg.buildcfg}"

  filter "configurations:Debug"
    defines { "DEBUG" }
    flags { "Symbols" }
    -- symbols "On"

  filter "configurations:Release"
    defines { "NDEBUG" }
    flags { "Optimize" }
    optimize "On"

  filter "system:windows"
    defines { "WIN32", "WIN64" }

  ------------------------
  -- liblmdb project
  ------------------------
  project "liblmdb"
    kind "StaticLib"
    language "C++"
    flags { "C++14" }
    files { "./%{prj.name}/*.c" }        
    includedirs { "./%{prj.name}" }
