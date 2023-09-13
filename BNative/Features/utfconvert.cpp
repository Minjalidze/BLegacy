#include "utfconvert.h"

string utf16_to_utf8(const u16string &s)
{
	wstring_convert<codecvt_utf8_utf16<char16_t>, char16_t> conv;
	return conv.to_bytes(s);
}

string utf32_to_utf8(const u32string &s)
{
	wstring_convert<codecvt_utf8<char32_t>, char32_t> conv;
	return conv.to_bytes(s);
}

std::u16string utf8_to_utf16(const string &s)
{
	wstring_convert<codecvt_utf8_utf16<char16_t>, char16_t> conv;
	return conv.from_bytes(s);
}

std::u16string utf32_to_utf16(const u32string &s)
{
	wstring_convert<codecvt_utf16<char32_t>, char32_t> conv;
	string bytes = conv.to_bytes(s);
	return u16string(reinterpret_cast<const char16_t*>(bytes.c_str()), bytes.length() / sizeof(char16_t));
}

std::u32string utf8_to_utf32(const string &s)
{
	wstring_convert<codecvt_utf8<char32_t>, char32_t> conv;
	return conv.from_bytes(s);
}

std::u32string utf16_to_utf32(const u16string &s)
{
	const char16_t *pData = s.c_str();
	wstring_convert<codecvt_utf16<char32_t>, char32_t> conv;
	return conv.from_bytes(reinterpret_cast<const char*>(pData), reinterpret_cast<const char*>(pData + s.length()));
}
