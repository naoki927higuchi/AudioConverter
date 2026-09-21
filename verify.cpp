#include <windows.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <wrl.h>
#include <stdio.h>
#include <vector>
using Microsoft::WRL::ComPtr;
int wmain(int argc, wchar_t** argv) {
 if (argc < 4) return 10;
 CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
 CLSID cls{}; CLSIDFromString(L"{BC7FDB46-BECF-4455-BA27-805281C23497}", &cls);
 ComPtr<IExplorerCommand> command;
 HRESULT hr;
 bool direct = argc > 4 && !wcscmp(argv[4], L"--direct");
 if (direct) {
  auto dll = LoadLibraryW(L"dist\\AudioConverterCommand.dll"); if (!dll) return 11;
  using GetFactory = HRESULT(__stdcall*)(REFCLSID, REFIID, void**);
  auto getFactory = reinterpret_cast<GetFactory>(GetProcAddress(dll,"DllGetClassObject")); if(!getFactory)return 12;
  ComPtr<IClassFactory> factory; hr = getFactory(cls,IID_PPV_ARGS(&factory)); if(FAILED(hr))return 13;
  hr = factory->CreateInstance(nullptr,IID_PPV_ARGS(&command));
 } else hr = CoCreateInstance(cls, nullptr, CLSCTX_LOCAL_SERVER, IID_PPV_ARGS(&command));
 if (FAILED(hr)) { wprintf(L"COM activation failed: %08X\n", (unsigned)hr); return 1; }
 PWSTR title = nullptr; hr = command->GetTitle(nullptr, &title); if (FAILED(hr)) return 2;
 wprintf(L"COM activated; title: %s\n", title); bool correctTitle = !wcscmp(title,L"MP3に変換"); CoTaskMemFree(title); if(!correctTitle) return 3;
 std::vector<PIDLIST_ABSOLUTE> ids;
 for (int i = 1; i < 4; i++) { PIDLIST_ABSOLUTE id = nullptr; hr = SHParseDisplayName(argv[i], nullptr, &id, 0, nullptr); if (FAILED(hr)) return 4; ids.push_back(id); }
 for (int count : {1,2,3}) {
  ComPtr<IShellItemArray> items; hr = SHCreateShellItemArrayFromIDLists(count, const_cast<PCIDLIST_ABSOLUTE*>(ids.data()), &items); if(FAILED(hr)) return 5;
  EXPCMDSTATE state{}; hr = command->GetState(items.Get(), TRUE, &state);
  wprintf(L"selection count=%d state=%d hr=%08X\n", count, state, (unsigned)hr);
  if (FAILED(hr) || state != static_cast<EXPCMDSTATE>(count == 3 ? ECS_HIDDEN : ECS_ENABLED)) return 6;
  if (count == 2 && argc > 4 && !wcscmp(argv[4], L"--invoke")) { hr = command->Invoke(items.Get(),nullptr); if(FAILED(hr))return 7; wprintf(L"Invoked two WAVs in one launch.\n"); }
 }
 for(auto id: ids) CoTaskMemFree(id);
 return 0;
}
