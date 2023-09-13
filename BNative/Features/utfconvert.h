#ifndef _EXT_UTF_CONVERT_H_
#define _EXT_UTF_CONVERT_H_

#include <codecvt>
#include <locale>
#include <string>

using namespace std;

string utf16_to_utf8(const u16string &s);
string utf32_to_utf8(const u32string &s);
u16string utf8_to_utf16(const string &s);
u16string utf32_to_utf16(const u32string &s);
u32string utf8_to_utf32(const string &s);
u32string utf16_to_utf32(const u16string &s);

template <typename T>
void utf8_to_utf16(const string& source, basic_string<T, char_traits<T>, allocator<T>>& result)
{
	wstring_convert<codecvt_utf8_utf16<T>, T> convert;
	result = convert.from_bytes(source);
}

template <typename T>
string utf16_to_utf8(const basic_string<T, char_traits<T>, allocator<T>>& source)
{
	wstring_convert<codecvt_utf8_utf16<T>, T> convert;
	return convert.to_bytes(source);
}

#endif