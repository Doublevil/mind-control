using MindControl.Results;
using MindControl.Threading;

namespace MindControl;

// This partial class implements the thread-related features of ProcessMemory.
public partial class ProcessMemory
{
    #region Public methods
    
    /// <summary>
    /// Starts a new thread in the target process, running the function at the specified address. This method will not
    /// wait for the thread to finish execution. Use the resulting <see cref="RemoteThread"/> instance to wait for the
    /// thread to finish if you need to.
    /// </summary>
    /// <param name="functionAddress">Address of the function (or start instruction) to run in the target process.
    /// </param>
    /// <param name="parameter">An optional parameter to pass to the function. It can be either an address or a value,
    /// depending on the function parameters. If the function takes multiple or complex parameters, consider using
    /// <see cref="RunThreadWithStoredParameter{T}(System.UIntPtr,T)"/> and/or using a structure holding each parameter
    /// with a matching field order.</param>
    /// <returns>A result holding either the thread instance that you can use to wait for the thread to return, or a
    /// <see cref="ThreadFailure"/> error.</returns>
    public Result<RemoteThread, ThreadFailure> RunThread(UIntPtr functionAddress, UIntPtr? parameter = null)
    {
        var startResult = StartThread(functionAddress, parameter ?? UIntPtr.Zero);
        if (startResult.IsFailure)
            return startResult.Error;
        return new RemoteThread(_osService, startResult.Value);
    }

    /// <summary>
    /// Starts a new thread in the target process, running the function at the address pointed by the given pointer
    /// path. This method will not wait for the thread to finish execution. Use the resulting
    /// <see cref="RemoteThread"/> instance to wait for the thread to finish if you need to.
    /// </summary>
    /// <param name="functionPointerPath">Pointer path to the function (or start instruction) to run in the target
    /// process.</param>
    /// <param name="parameter">An optional parameter to pass to the function. It can be either an address or a value,
    /// depending on the function parameters. If the function takes multiple or complex parameters, consider using
    /// <see cref="RunThreadWithStoredParameter{T}(PointerPath,T)"/> and/or using a structure holding each parameter
    /// with a matching field order.</param>
    /// <returns>A result holding either the thread instance that you can use to wait for the thread to return, or a
    /// <see cref="ThreadFailure"/> error.</returns>
    public Result<RemoteThread, ThreadFailure> RunThread(PointerPath functionPointerPath, UIntPtr? parameter = null)
    {
        var evaluateResult = EvaluateMemoryAddress(functionPointerPath);
        if (evaluateResult.IsFailure)
            return new ThreadFailureOnPointerPathEvaluation(evaluateResult.Error);
        return RunThread(evaluateResult.Value, parameter);
    }
    
    /// <summary>
    /// Starts a new thread in the target process, running the specified exported function from a module loaded into the
    /// target process. This method will not wait for the thread to finish execution. Use the resulting
    /// <see cref="RemoteThread"/> instance to wait for the thread to finish if you need to.
    /// </summary>
    /// <param name="moduleName">Name of the module containing the function to run (e.g. "kernel32.dll").</param>
    /// <param name="functionName">Name of the exported function to run from the specified module.</param>
    /// <param name="parameter">An optional parameter to pass to the function. It can be either an address or a value,
    /// depending on the function parameters. If the function takes multiple or complex parameters, consider using
    /// <see cref="RunThreadWithStoredParameter{T}(string,string,T)"/> and/or using a structure holding each parameter
    /// with a matching field order.</param>
    /// <returns>A result holding either the thread instance that you can use to wait for the thread to return, or a
    /// <see cref="ThreadFailure"/> error.</returns>
    public Result<RemoteThread, ThreadFailure> RunThread(string moduleName, string functionName,
        UIntPtr? parameter = null)
    {
        var functionAddressResult = FindFunctionAddress(moduleName, functionName);
        if (functionAddressResult.IsFailure)
            return functionAddressResult.Error;
        
        return RunThread(functionAddressResult.Value, parameter);
    }

    /// <summary>
    /// Stores the given parameter in the target process memory, and then starts a new thread in the target process,
    /// running the function at the specified address. This method will not wait for the thread to finish execution.
    /// Use the resulting <see cref="RemoteThread"/> instance to wait for the thread to finish if you need to.
    /// </summary>
    /// <param name="functionAddress">Address of the function (or start instruction) to run in the target process.
    /// </param>
    /// <param name="parameter">Parameter to pass to the function. If the function takes multiple parameters, they can
    /// be arranged in a structure holding each value in the right field order.</param>
    /// <returns>A result holding either the thread instance that you can use to wait for the thread to return, or a
    /// <see cref="ThreadFailure"/> error.</returns>
    public Result<ParameterizedRemoteThread, ThreadFailure> RunThreadWithStoredParameter<T>(UIntPtr functionAddress,
        T parameter)
    {
        var storeResult = Store(parameter);
        if (storeResult.IsFailure)
            return new ThreadFailureOnParameterStorageFailure(storeResult.Error);

        var reservation = storeResult.Value;
        var startResult = StartThread(functionAddress, reservation.Address);
        if (startResult.IsFailure)
            return startResult.Error;
        return new ParameterizedRemoteThread(_osService, startResult.Value, reservation);
    }
    
    /// <summary>
    /// Stores the given parameter in the target process memory, and then starts a new thread in the target process,
    /// running the function at the address pointed by the specified pointer path. This method will not wait for the
    /// thread to finish execution. Use the resulting <see cref="RemoteThread"/> instance to wait for the thread to
    /// finish if you need to.
    /// </summary>
    /// <param name="functionPointerPath">Pointer path to the function (or start instruction) to run in the target
    /// process.</param>
    /// <param name="parameter">Parameter to pass to the function. If the function takes multiple parameters, they can
    /// be arranged in a structure holding each value in the right field order.</param>
    /// <returns>A result holding either the thread instance that you can use to wait for the thread to return, or a
    /// <see cref="ThreadFailure"/> error.</returns>
    public Result<ParameterizedRemoteThread, ThreadFailure> RunThreadWithStoredParameter<T>(
        PointerPath functionPointerPath, T parameter)
    {
        var evaluateResult = EvaluateMemoryAddress(functionPointerPath);
        if (evaluateResult.IsFailure)
            return new ThreadFailureOnPointerPathEvaluation(evaluateResult.Error);
        return RunThreadWithStoredParameter(evaluateResult.Value, parameter);
    }
    
    /// <summary>
    /// Stores the given parameter in the target process memory, and then starts a new thread in the target process,
    /// running the specified exported function from a module loaded into the target process. This method will not wait
    /// for the thread to finish execution. Use the resulting <see cref="RemoteThread"/> instance to wait for the
    /// thread to finish if you need to.
    /// </summary>
    /// <param name="moduleName">Name of the module containing the function to run (e.g. "kernel32.dll").</param>
    /// <param name="functionName">Name of the exported function to run from the specified module.</param>
    /// <param name="parameter">Parameter to pass to the function. If the function takes multiple parameters, they can
    /// be arranged in a structure holding each value in the right field order.</param>
    /// <returns>A result holding either the thread instance that you can use to wait for the thread to return, or a
    /// <see cref="ThreadFailure"/> error.</returns>
    public Result<ParameterizedRemoteThread, ThreadFailure> RunThreadWithStoredParameter<T>(string moduleName,
        string functionName, T parameter)
    {
        var functionAddressResult = FindFunctionAddress(moduleName, functionName);
        if (functionAddressResult.IsFailure)
            return functionAddressResult.Error;
        
        return RunThreadWithStoredParameter(functionAddressResult.Value, parameter);
    }
    
    #endregion
    
    #region Internal methods
    
    /// <summary>
    /// Starts a new thread in the target process, running the function at the specified address.
    /// </summary>
    /// <param name="functionAddress">Address of the function (or start instruction) to run in the target process.
    /// </param>
    /// <param name="parameter">Parameter to pass to the function. Use <see cref="UIntPtr.Zero"/> if the function does
    /// not take parameters.</param>
    /// <returns>A result holding either the handle to the thread, or a <see cref="ThreadFailure"/> error.</returns>
    private Result<IntPtr, ThreadFailure> StartThread(UIntPtr functionAddress, UIntPtr parameter)
    {
        var remoteThreadResult = _osService.CreateRemoteThread(ProcessHandle, functionAddress, parameter);
        if (remoteThreadResult.IsFailure)
            return new ThreadFailureOnSystemFailure("Failed to create the thread.", remoteThreadResult.Error);
        return remoteThreadResult.Value;
    }
    
    /// <summary>
    /// Finds the address of the specified function in the export table of the specified module.
    /// </summary>
    /// <param name="moduleName">Name of the module containing the function to find.</param>
    /// <param name="functionName">Name of the function to find in the export table of the module.</param>
    /// <returns>A result holding either the address of the function, or a <see cref="ThreadFailure"/> error.</returns>
    private Result<UIntPtr, ThreadFailure> FindFunctionAddress(string moduleName, string functionName)
    {
        var module = GetModule(moduleName);
        if (module == null)
            return new ThreadFailureOnFunctionNotFound($"The module \"{moduleName}\" is not loaded in the process.");
        var exportTable = module.ReadExportTable();
        if (exportTable.IsFailure)
            return new ThreadFailureOnFunctionNotFound(
                $"Failed to read the export table of the module \"{moduleName}\": {exportTable.Error}.");
        if (!exportTable.Value.TryGetValue(functionName, out UIntPtr functionAddress))
            return new ThreadFailureOnFunctionNotFound(
                $"The function \"{functionName}\" was not found in the export table of the module \"{moduleName}\".");
        return functionAddress;
    }
    
    #endregion
}