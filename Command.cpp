#include <windows.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <wrl.h>
#include <wrl/module.h>
#include <string>
using namespace Microsoft::WRL;
static HMODULE moduleHandle;
BOOL WINAPI DllMain(HINSTANCE module, DWORD reason, LPVOID) { if (reason == DLL_PROCESS_ATTACH) { moduleHandle = module; DisableThreadLibraryCalls(module); } return TRUE; }
static std::wstring ExePath() {
 wchar_t path[32768]{};
 GetModuleFileNameW(moduleHandle, path, 32768);
 std::wstring full(path); return full.substr(0, full.find_last_of(L'\\') + 1) + L"AudioConverter.exe";
}
class __declspec(uuid("BC7FDB46-BECF-4455-BA27-805281C23497")) Command final : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IExplorerCommand> {
public:
 HRESULT STDMETHODCALLTYPE GetTitle(IShellItemArray*, PWSTR* p) override { return SHStrDupW(L"MP3に変換", p); }
 HRESULT STDMETHODCALLTYPE GetIcon(IShellItemArray*, PWSTR* p) override { return SHStrDupW((ExePath() + L",0").c_str(), p); }
 HRESULT STDMETHODCALLTYPE GetToolTip(IShellItemArray*, PWSTR* p) override { return SHStrDupW(L"選択したWAVをAudioConverterに追加", p); }
 HRESULT STDMETHODCALLTYPE GetCanonicalName(GUID* p) override { *p = __uuidof(Command); return S_OK; }
 HRESULT STDMETHODCALLTYPE GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) override {
  *state = ECS_HIDDEN; DWORD count = 0;
  if (!items || FAILED(items->GetCount(&count)) || !count) return S_OK;
  for (DWORD i = 0; i < count; i++) {
   ComPtr<IShellItem> item; if (FAILED(items->GetItemAt(i, &item))) return S_OK;
   PWSTR path = nullptr; if (FAILED(item->GetDisplayName(SIGDN_FILESYSPATH, &path))) return S_OK;
   DWORD attrs = GetFileAttributesW(path);
   bool wav = !_wcsicmp(PathFindExtensionW(path), L".wav") && attrs != INVALID_FILE_ATTRIBUTES && !(attrs & FILE_ATTRIBUTE_DIRECTORY);
   CoTaskMemFree(path); if (!wav) return S_OK;
  }
  *state = ECS_ENABLED; return S_OK;
 }
 HRESULT STDMETHODCALLTYPE Invoke(IShellItemArray* items, IBindCtx*) override {
  try { return Launch(items); } catch (...) { return E_FAIL; }
 }
 HRESULT Launch(IShellItemArray* items) {
  EXPCMDSTATE state; GetState(items, TRUE, &state); if (state != ECS_ENABLED) return E_INVALIDARG;
  DWORD count = 0; HRESULT hr = items->GetCount(&count); if (FAILED(hr)) return hr;
  std::wstring selection = L"\xFEFF";
  for (DWORD i = 0; i < count; i++) {
   ComPtr<IShellItem> item; hr = items->GetItemAt(i, &item); if (FAILED(hr)) return hr;
   PWSTR path = nullptr; hr = item->GetDisplayName(SIGDN_FILESYSPATH, &path); if (FAILED(hr)) return hr;
   selection += path; selection += L"\r\n"; CoTaskMemFree(path);
  }
  PWSTR local = nullptr; hr = SHGetKnownFolderPath(FOLDERID_LocalAppData, KF_FLAG_DEFAULT, nullptr, &local); if (FAILED(hr)) return hr;
  std::wstring folder = std::wstring(local) + L"\\AudioConverter\\Requests"; CoTaskMemFree(local);
  int created = SHCreateDirectoryExW(nullptr, folder.c_str(), nullptr);
  if (created != ERROR_SUCCESS && created != ERROR_ALREADY_EXISTS) return HRESULT_FROM_WIN32(created);
  GUID guid; hr = CoCreateGuid(&guid); if (FAILED(hr)) return hr;
  wchar_t id[40]; StringFromGUID2(guid, id, 40);
  auto request = folder + L"\\selection-" + id + L".txt";
  HANDLE file = CreateFileW(request.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
  if (file == INVALID_HANDLE_VALUE) return HRESULT_FROM_WIN32(GetLastError());
  DWORD written = 0; DWORD bytes = static_cast<DWORD>(selection.size() * sizeof(wchar_t));
  BOOL ok = WriteFile(file, selection.data(), bytes, &written, nullptr); DWORD error = GetLastError(); CloseHandle(file);
  if (!ok || written != bytes) { DeleteFileW(request.c_str()); return HRESULT_FROM_WIN32(ok ? ERROR_WRITE_FAULT : error); }
  auto exe = ExePath(); auto args = L"\"" + exe + L"\" --selection \"" + request + L"\"";
  STARTUPINFOW startup{ sizeof(startup) }; PROCESS_INFORMATION process{};
  if (!CreateProcessW(exe.c_str(), args.data(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, &startup, &process)) {
   error = GetLastError(); DeleteFileW(request.c_str()); return HRESULT_FROM_WIN32(error);
  }
  CloseHandle(process.hThread); CloseHandle(process.hProcess); return S_OK;
 }
 HRESULT STDMETHODCALLTYPE GetFlags(EXPCMDFLAGS* p) override { *p = ECF_DEFAULT; return S_OK; }
 HRESULT STDMETHODCALLTYPE EnumSubCommands(IEnumExplorerCommand** p) override { *p = nullptr; return E_NOTIMPL; }
};
CoCreatableClass(Command);
extern "C" HRESULT __stdcall DllGetClassObject(REFCLSID cls, REFIID iid, void** result) { return Module<InProc>::GetModule().GetClassObject(cls, iid, result); }
extern "C" HRESULT __stdcall DllCanUnloadNow() { return Module<InProc>::GetModule().GetObjectCount() == 0 ? S_OK : S_FALSE; }
