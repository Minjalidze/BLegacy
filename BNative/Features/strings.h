#ifndef _EXT_STRINGS_H_
#define _EXT_STRINGS_H_

#define _CRT_SECURE_NO_WARNINGS

#include <windows.h>
#include <string>
#include <vector>

/*
// Array :: Length
*/

int array_length(void *_array);
void **array_new(void *values, ...);
void array_append(void ***_array, void *_value);
//void array_append(void **_array, int size, void *_value);

/*
// ToInteger ( from char* )
*/

inline short to_short(char* input) { return *((short *)input); }
inline short to_int16(char* input) { return *((short *)input); }

inline unsigned short to_ushort(char* input) { return *((unsigned short *)input); }
inline unsigned short to_uint16(char* input) { return *((unsigned short *)input); }

inline int to_int(char* input) { return *((int *)input); }
inline int to_int32(char* input) { return *((int *)input); }
inline long to_long(char* input) { return *((long *)input); }

inline unsigned int to_uint(char* input) { return *((unsigned int *)input); }
inline unsigned int to_uint32(char* input) { return *((unsigned int *)input); }
inline unsigned long to_ulong(char* input) { return *((unsigned long *)input); }

inline long long to_int64(char* input) { return *((long long *)input); }
inline long long to_longlong(char* input) { return *((long long *)input); }

inline unsigned long long to_uint64(char* input) { return *((unsigned long long *)input); }
inline unsigned long long to_ulonglong(char* input) { return *((unsigned long long *)input); }

/*
// String :: ToLower
*/

char *to_lower(char *value);
char *to_upper(char *value);

wchar_t *to_lower(wchar_t *value);
wchar_t *to_upper(wchar_t *value);

/*
// String :: ToString
*/

char *to_cstring(std::string input);
#if __cplusplus <= 201402L
std::string to_string(void *value);
std::string to_string(char value);
std::string to_string(unsigned char value);
std::string to_string(short value);
std::string to_string(unsigned short value);
std::string to_string(int value);
std::string to_string(unsigned int value);
std::string to_string(long value);
std::string to_string(unsigned long value);
#if __cplusplus < 199711L
std::string to_string(long long value);
std::string to_string(unsigned long long value);
#endif
std::string to_string(float value);
std::string to_string(double value);
#if __cplusplus < 199711L
std::string to_string(long double value);
#endif
#endif

/*
// String : WCharString
*/

wchar_t* strtowcs(const char *value);
char* wcstostr(const wchar_t *value);

/*
// String : Search
*/

size_t strpos(const char* haystack, const char* needle);

/*
// String : Format
*/

char *formatf(const char *format, ...);
wchar_t *formatf(const wchar_t *format, ...);

/*
// String : Concat
*/

char* strconcat(const char *first, ...);
wchar_t* wcsconcat(const wchar_t *first, ...);

/*
// String : Trim
*/

char *trim(const char *input, const char *trimChars = " \t\n\r\f\v");
wchar_t *trim(const wchar_t *input, const wchar_t *trimChars = L" \t\n\r\f\v");

/*
// String : Replace
*/

char *replace(const char *input, const char from, const char to);
wchar_t *replace(const wchar_t *input, const wchar_t from, const wchar_t to);

/*
// String : Split String (from left to right)
*/

char **split(const char *input, const char separator, int max_chunks = 0);
char **split(const char *input, const char *separator, int max_chunks = 0);
wchar_t **split(const wchar_t *input, const wchar_t separator, int max_chunks = 0);
wchar_t **split(const wchar_t *input, const wchar_t *separator, int max_chunks = 0);

/*
// String : Split String (from right to left)
*/

char **split_r(const char *input, const char separator, int max_chunks = 0);
char **split_r(const char *input, const char *separator, int max_chunks = 0);
wchar_t **split_r(const wchar_t *input, const wchar_t separator, int max_chunks = 0);
wchar_t **split_r(const wchar_t *input, const wchar_t *separator, int max_chunks = 0);

/*
// String : Split Quoted String
*/

char** split_quotes(const char *input, const char separator = ' ', bool keep_quotes = false);
wchar_t** split_quotes(const wchar_t *input, const wchar_t separator = L' ', bool keep_quotes = false);

#endif