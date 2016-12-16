function mk_release_dir()
  if not os.isdir("release") then
    os.mkdir("release")
    os.mkdir("release/include")
    os.mkdir("release/lib")
  end
end

--
-- LMDB workspace
--
workspace "LMDB"
  configurations { "Debug", "Release" }
  platforms({"x64"})
  location("build/" .. _ACTION)
  startproject "liblmdb"
  targetdir "./bin/%{_ACTION}/%{cfg.buildcfg}"

  -- generate release
  mk_release_dir()

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

    -- target names
    filter "configurations:Debug"
      targetname "lmdb-d"
    
    filter "configurations:Release"
      targetname "lmdb"
  
    -- postbuild commands
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
      