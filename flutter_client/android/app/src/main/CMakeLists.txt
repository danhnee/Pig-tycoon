cmake_minimum_required(VERSION 3.18.1)
project("pig_tycoon_android_native")

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(
    pig_tycoon_cpp
    SHARED
    ../../../../../native_polyglot/cpp_core/pig_tycoon_kernels.cpp
)

target_compile_options(pig_tycoon_cpp PRIVATE -O3 -ffast-math)
