function mk_release_dir()
  if not os.isdir("release") then
    os.mkdir("release")
    os.mkdir("release/include")
    os.mkdir("release/lib")
  end
end

workspace "LMDB"
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
    -- flags { "Optimize" }
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

    -- generate release
    mk_release_dir()

    postbuildcommands {
      "{COPY} \"../../%{prj.name}/lmdb.h\" \"../../release/include\"",
    }

    filter "system:windows"
      filter "configurations:Debug"
        postbuildcommands {
          "{COPY} \"../../bin/%{_ACTION}/%{cfg.buildcfg}/lmdb-d.lib\" \"../../release/lib\"",
        }
      filter "configurations:Release"
        postbuildcommands {
          "{COPY} \"../../bin/%{_ACTION}/%{cfg.buildcfg}/lmdb.lib\" \"../../release/lib\"",
        }
      