/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using SAM.Core.ViewModels;

namespace SAM.Core.Tests.ViewModels;

public class ViewModelBaseTests
{
    [Fact]
    public async Task ExecuteWithBusyAsync_SetsIsBusyDuringExecution()
    {
        var vm = new TestViewModel();
        var gate = new TaskCompletionSource<bool>();
        var started = new TaskCompletionSource<bool>();

        var task = vm.RunBusy(async _ =>
        {
            started.SetResult(true);
            await gate.Task;
        });

        await started.Task;
        Assert.True(vm.IsBusy);

        gate.SetResult(true);
        await task;

        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ExecuteWithBusyAsync_PreventsConcurrentExecution()
    {
        var vm = new TestViewModel { IsBusy = true };
        var callCount = 0;

        await vm.RunBusy(_ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task ExecuteWithBusyAsync_CancellationDoesNotSetError()
    {
        var vm = new TestViewModel();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await vm.RunBusy(token =>
        {
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }, cts.Token);

        Assert.False(vm.HasError);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteWithBusyAsync_ExceptionSetsError()
    {
        var vm = new TestViewModel();

        await vm.RunBusy(_ => throw new Exception("Steam is not running"));

        Assert.True(vm.HasError);
        Assert.False(string.IsNullOrWhiteSpace(vm.ErrorMessage));
    }

    [Fact]
    public void CancelOperations_CancelsToken()
    {
        var vm = new TestViewModel();
        var token = vm.NewToken();

        Assert.False(token.IsCancellationRequested);
        vm.CancelOperations();
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void GetOperationCancellationToken_CancelsPreviousToken()
    {
        var vm = new TestViewModel();
        var first = vm.NewToken();
        var second = vm.NewToken();

        Assert.True(first.IsCancellationRequested);
        Assert.False(second.IsCancellationRequested);
    }

    [Fact]
    public void SetErrorAndClearError_UpdateHasErrorState()
    {
        var vm = new TestViewModel();

        vm.SetErrorPublic("oops");
        Assert.True(vm.HasError);
        Assert.Equal("oops", vm.ErrorMessage);

        vm.ClearErrorPublic();
        Assert.False(vm.HasError);
        Assert.Null(vm.ErrorMessage);
    }

    private sealed class TestViewModel : ViewModelBase
    {
        public Task RunBusy(Func<CancellationToken, Task> action, CancellationToken token = default)
            => ExecuteWithBusyAsync(action, token);

        public void SetErrorPublic(string? message) => SetError(message);

        public void ClearErrorPublic() => ClearError();

        public CancellationToken NewToken() => GetOperationCancellationToken();
    }
}
