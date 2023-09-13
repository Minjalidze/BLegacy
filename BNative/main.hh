#pragma once
#include <Windows.h>
#include <Psapi.h>
#include <iostream>
#include <sstream>
#include <conio.h>
#include <psapi.h>
#include <thread>

#define PATH_SEPARATOR "\\" 
#define GetCurrentDirA _getcwd
#define GetCurrentDirW _wgetcwd

#include "mono/Mono.h"
#include "Features/crc32.h"
#include "Features/file.h"
