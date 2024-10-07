using System;
using System.Runtime.InteropServices;

using Fahrenheit.CoreLib;

using ImGuiNET;

using static Fahrenheit.CoreLib.FhHookDelegates;
using static Fahrenheit.Modules.Core.PInvoke;

namespace Fahrenheit.Modules.Core;

public sealed record FhImguiModuleConfig : FhModuleConfig {
    public FhImguiModuleConfig(string configName, bool configEnabled) : base(configName, configEnabled) { }

    public override FhModule SpawnModule() {
        return new FhImguiModule(this);
    }
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public delegate nint graphicInitialize();

/* [fkelava 6/10/24 01:54]
 * SharpDX reference:
 *
 *  /// <unmanaged>ULONG IUnknown::Release()</unmanaged>
 *  /// <unmanaged-short>IUnknown::Release</unmanaged-short>
 *  public unsafe uint Release()
 *  {
 *      uint __result__;
 *      __result__ = ((delegate* unmanaged[Stdcall]<IntPtr, uint>) this[2U])(NativePointer);
 *      return __result__;
 *  }
 */
public unsafe delegate uint ComIUnknown_Release(nint* p_object);

/* [fkelava 6/10/24 01:54]
 * public unsafe Result Present(int syncInterval, PresentFlags flags)
 * {
 *     return ((delegate* unmanaged[Stdcall]<nint, int, int, int>)base[8])(base.NativePointer, syncInterval, (int)flags);
 * }
 */

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate nint DXGISwapChain_Present(nint* pSwapChain, uint SyncInterval, uint Flags);

/* [fkelava 6/10/24 01:54]
 * SharpDX reference:
 *
 * private unsafe Result GetBuffer(int buffer, Guid riid, out nint surface)
 * {
 *     Result result;
 *     fixed (nint* ptr = &surface)
 *     {
 *         void* ptr2 = ptr;
 *         result = ((delegate* unmanaged[Stdcall]<nint, int, void*, void*, int>)base[9])(base.NativePointer, buffer, &riid, ptr2);
 *     }
 *
 *     return result;
 * }
 *
 * where
 * [Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c")]
 * public class ID3D11Texture2D : ID3D11Resource
 */

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate nint DXGISwapChain_GetBuffer(nint* pSwapChain, uint Buffer, void* riid, nint** ppSurface);

/* [fkelava 6/10/24 01:54]
 * SharpDX reference:
 *
 * public unsafe ID3D11RenderTargetView CreateRenderTargetView(ID3D11Resource resource, RenderTargetViewDescription? desc = null)
 * {
 *     nint zero = IntPtr.Zero;
 *     nint zero2 = IntPtr.Zero;
 *     zero = resource?.NativePointer ?? IntPtr.Zero;
 *     RenderTargetViewDescription value = default(RenderTargetViewDescription);
 *     if (desc.HasValue)
 *     {
 *         value = desc.Value;
 *     }
 *
 *     Result result = ((delegate* unmanaged[Stdcall]<nint, void*, void*, void*, int>)base[9])(base.NativePointer, (void*)zero, (!desc.HasValue) ? null : (&value), &zero2);
 *     ID3D11RenderTargetView result2 = ((zero2 != IntPtr.Zero) ? new ID3D11RenderTargetView(zero2) : null);
 *     GC.KeepAlive(resource);
 *     result.CheckError();
 *     return result2;
 * }
 */

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate nint D3D11Device_CreateRenderTargetView(nint* pDevice, nint* pResource, nint* pDesc, nint** ppRTView);

/* [fkelava 6/10/24 01:54]
 * SharpDX reference:
 *
 * private unsafe void OMSetRenderTargets(int numViews, void* renderTargetViews, ID3D11DepthStencilView depthStencilView)
 * {
 *     nint zero = IntPtr.Zero;
 *     zero = depthStencilView?.NativePointer ?? IntPtr.Zero;
 *     ((delegate* unmanaged[Stdcall]<nint, int, void*, void*, void>)base[33])(base.NativePointer, numViews, renderTargetViews, (void*)zero);
 *     GC.KeepAlive(depthStencilView);
 * }
 */

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public unsafe delegate nint D3D11DeviceContext_OMSetRenderTargets(nint* pDeviceContext, uint NumViews, nint** ppRenderTargetViews, nint* pDepthStencilView);

public unsafe class FhImguiModule : FhModule {
    // WndProc support
    private          nint            _hWnd;
    private          nint            _ptr_o_WndProc;
    private          nint            _ptr_h_WndProc;
    private readonly WndProcDelegate _h_WndProc;

    // D3D11/DXGI support
    private const int _D3D11_VTBL_SWAP_CHAIN_COUNT       = 18;
    private const int _D3D11_VTBL_DEVICE_COUNT           = 43;
    private const int _D3D11_VTBL_DEVICE_CTX_COUNT       = 144;
    private const int _D3D11_VTBL_D3D11_TEXTURE_2D_COUNT = 3; // incorrect, but we don't need any method beyond ordinal 2 - IUnknown::Release()

    private          bool                                          _present_init_complete;         // has h_present finished one-time initialization?
    private          DXGISwapChain_GetBuffer?                      _swap_chain_GetBuffer;          // _p_swap_chain->vtbl[9]  https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-getbuffer
    private          D3D11Device_CreateRenderTargetView?           _device_CreateRenderTargetView; // _p_device    ->vtbl[9]  https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-createrendertargetview
    private          D3D11DeviceContext_OMSetRenderTargets?        _device_ctx_OMSetRenderTargets; // _p_device_ctx->vtbl[33] https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11devicecontext-omsetrendertargets
    private          nint*                                         _p_swap_chain;                  // IDXGISwapChain*         https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nn-dxgi-idxgiswapchain
    private          nint*                                         _p_device;                      // ID3D11Device*           https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nn-d3d11-id3d11device
    private          nint*                                         _p_device_ctx;                  // ID3D11DeviceContext*    https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nn-d3d11-id3d11devicecontext
    private          nint*                                         _p_surface;                     // ID3D11Texture2D*        https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nn-d3d11-id3d11texture2d
    private          nint*                                         _p_render_target_view;          // ID3D11RenderTargetView* https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nn-d3d11-id3d11rendertargetview
    private readonly nint[]                                        _vtbl_swap_chain;               // _p_swap_chain->vtbl[]
    private readonly nint[]                                        _vtbl_device;                   // _p_device    ->vtbl[]
    private readonly nint[]                                        _vtbl_device_ctx;               // _p_device_ctx->vtbl[]
    private readonly nint[]                                        _vtbl_d3d11_texture_2d;         // _p_surface   ->vtbl[]
    private          bool                                          _present_ready;                 // Phyre is not ready to render until the 'FINAL FANTASY X PROJECT' logo i.e. the main loop has run at least once.

    private readonly FhMethodHandle<Sg_MainLoop>                   _handle_main_loop;
    private readonly FhMethodHandle<graphicInitialize>             _handle_wndproc_init;
    private readonly FhMethodHandle<D3D11CreateDeviceAndSwapChain> _handle_d3d11_init;
    private          FhMethodHandle<DXGISwapChain_Present>?        _handle_present;

    public FhImguiModule(FhImguiModuleConfig cfg) : base(cfg) {
        _vtbl_swap_chain       = new nint[_D3D11_VTBL_SWAP_CHAIN_COUNT];
        _vtbl_device           = new nint[_D3D11_VTBL_DEVICE_COUNT];
        _vtbl_device_ctx       = new nint[_D3D11_VTBL_DEVICE_CTX_COUNT];
        _vtbl_d3d11_texture_2d = new nint[_D3D11_VTBL_D3D11_TEXTURE_2D_COUNT];

        _handle_main_loop      = new(this, "FFX.exe",   h_main_loop,    offset:  0x420C00);
        _handle_wndproc_init   = new(this, "FFX.exe",   h_init_wndproc, offset:  0x241B80);
        _handle_d3d11_init     = new(this, "D3D11.dll", h_init_d3d11,   fn_name: "D3D11CreateDeviceAndSwapChain");
        _h_WndProc             = h_wndproc;
    }

    public override bool init() {
        return _handle_d3d11_init  .hook() &&
               _handle_wndproc_init.hook() &&
               _handle_main_loop   .hook();
    }

    private void init_imgui() {
        if (_hWnd == 0        ||                        // h_init_wndproc hasn't run yet?
            _p_device == null || _p_device_ctx == null) // h_init_d3d11 hasn't run yet?
            return;

        ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();

        // Enable controls
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;

        ImGui.StyleColorsDark();

        ImGui.ImGui_ImplWin32_Init(_hWnd);
        ImGui.ImGui_ImplDX11_Init (_p_device, _p_device_ctx);
    }

    private nint h_init_wndproc() {
        nint result = _handle_wndproc_init.orig_fptr.Invoke();

        _hWnd          = FhUtil.get_at<nint>(0x8C9CE8);
        _ptr_h_WndProc = Marshal.GetFunctionPointerForDelegate(_h_WndProc);
        _ptr_o_WndProc = PInvoke.GetWindowLongA(_hWnd, PInvoke.GWLP_WNDPROC);
        int _          = PInvoke.SetWindowLongA(_hWnd, PInvoke.GWLP_WNDPROC, _ptr_h_WndProc);

        init_imgui();
        return result;
    }

    public nint h_init_d3d11(
        nint*  pAdapter,
        nint   DriverType,
        nint   Software,
        uint   Flags,
        nint*  pFeatureLevels,
        uint   FeatureLevels,
        uint   SDKVersion,
        nint*  pSwapChainDesc,
        nint** ppSwapChain,
        nint** ppDevice,
        nint*  pFeatureLevel,
        nint** ppImmediateContext) {

        nint result = _handle_d3d11_init.orig_fptr.Invoke
            (pAdapter, DriverType, Software, Flags, pFeatureLevels, FeatureLevels, SDKVersion, pSwapChainDesc, ppSwapChain, ppDevice, pFeatureLevel, ppImmediateContext);

        if (result != 0) return result; // S_FALSE is a possible return

        fixed (nint* vtbl_swap_chain_arr_ptr = _vtbl_swap_chain)
        fixed (nint* vtbl_device_arr_ptr     = _vtbl_device)
        fixed (nint* vtbl_device_ctx_arr_ptr = _vtbl_device_ctx) {
            Buffer.MemoryCopy((**ppSwapChain)       .ToPointer(), vtbl_swap_chain_arr_ptr, _D3D11_VTBL_SWAP_CHAIN_COUNT * sizeof(nint), _D3D11_VTBL_SWAP_CHAIN_COUNT * sizeof(nint));
            Buffer.MemoryCopy((**ppDevice)          .ToPointer(), vtbl_device_arr_ptr,     _D3D11_VTBL_DEVICE_COUNT     * sizeof(nint), _D3D11_VTBL_DEVICE_COUNT     * sizeof(nint));
            Buffer.MemoryCopy((**ppImmediateContext).ToPointer(), vtbl_device_ctx_arr_ptr, _D3D11_VTBL_DEVICE_CTX_COUNT * sizeof(nint), _D3D11_VTBL_DEVICE_CTX_COUNT * sizeof(nint));
        }

        _p_swap_chain = *ppSwapChain;
        _p_device     = *ppDevice;
        _p_device_ctx = *ppImmediateContext;

        _swap_chain_GetBuffer          = Marshal.GetDelegateForFunctionPointer<DXGISwapChain_GetBuffer>              (_vtbl_swap_chain[9]);
        _device_CreateRenderTargetView = Marshal.GetDelegateForFunctionPointer<D3D11Device_CreateRenderTargetView>   (_vtbl_device[9]);
        _device_ctx_OMSetRenderTargets = Marshal.GetDelegateForFunctionPointer<D3D11DeviceContext_OMSetRenderTargets>(_vtbl_device_ctx[33]);

        _handle_present = new FhMethodHandle<DXGISwapChain_Present>(this, _vtbl_swap_chain[8], h_present);
        _handle_present.hook();

        init_imgui();
        return result;
    }

    public void h_main_loop(float delta) {
        _handle_main_loop.orig_fptr(delta);
        _present_ready = true;
    }

    public nint h_wndproc(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam) {
        return PInvoke.ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam) == 1
            ? 1
            : PInvoke.CallWindowProcW(_ptr_o_WndProc, hWnd, msg, wParam, lParam);
    }

    public nint h_present(nint* pSwapChain, uint SyncInterval, uint Flags) {
        if (_hWnd == 0                                                                                                        || // h_init_wndproc hasn't run yet?
            _swap_chain_GetBuffer == null || _device_CreateRenderTargetView == null || _device_ctx_OMSetRenderTargets == null || // h_init_d3d11 hasn't run yet?
            !_present_ready)                                                                                                     // game main loop hasn't run yet?
            return _handle_present!.orig_fptr(pSwapChain, SyncInterval, Flags);

        if (!_present_init_complete) {
            Guid _guid_d3d11texture2d = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

            fixed (nint** ppSurface                 = &_p_surface)
            fixed (nint** ppRenderTargetView        = &_p_render_target_view)
            fixed (nint*  vtbl_d3d11_texture_2d_ptr = _vtbl_d3d11_texture_2d) {
                _ = _swap_chain_GetBuffer         (_p_swap_chain, 0, &_guid_d3d11texture2d, ppSurface);
                _ = _device_CreateRenderTargetView(_p_device, _p_surface, null, ppRenderTargetView);

                Buffer.MemoryCopy((*_p_surface).ToPointer(), vtbl_d3d11_texture_2d_ptr, _D3D11_VTBL_D3D11_TEXTURE_2D_COUNT * sizeof(nint), _D3D11_VTBL_D3D11_TEXTURE_2D_COUNT * sizeof(nint));

                ComIUnknown_Release com_release = Marshal.GetDelegateForFunctionPointer<ComIUnknown_Release>(_vtbl_d3d11_texture_2d[2]);
                com_release(_p_surface);
            }

            _present_init_complete = true;
        }

        ImGui.ImGui_ImplDX11_NewFrame();
        ImGui.ImGui_ImplWin32_NewFrame();

        // TODO: Eve can clean this up, or rather wire this up
        ImGui.NewFrame();
        ImGui.Begin("Fahrenheit");
        ImGui.End();

        ImGui.Render();

        fixed (nint** ppRenderTargetView = &_p_render_target_view) {
            _device_ctx_OMSetRenderTargets(_p_device_ctx, 1, ppRenderTargetView, null);
        }

        ImGui.ImGui_ImplDX11_RenderDrawData(ImGui.GetDrawData());
        return _handle_present!.orig_fptr(pSwapChain, SyncInterval, Flags);
    }
}