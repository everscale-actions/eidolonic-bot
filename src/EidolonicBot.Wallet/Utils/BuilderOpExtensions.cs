using EverscaleNet.Client.Models;
using EverscaleNet.Serialization;

namespace EidolonicBot.Utils;

//todo: remove after releasing https://github.com/everscale-actions/everscale-dotnet/commit/9340357514004a6403d9ff2d7dd66f286cf0798d
public static class BuilderOpExtensions {
    public static BuilderOp ToBuilderOp(this bool b) {
        return new BuilderOp.Integer { Size = 1, Value = (b ? 1 : 0).ToJsonElement<int>() };
    }

    public static BuilderOp ToBuilderOp(this byte b) {
        return new BuilderOp.Integer { Size = 8, Value = b.ToJsonElement<byte>() };
    }

    public static BuilderOp ToBuilderOp(this short s) {
        return new BuilderOp.Integer { Size = 16, Value = s.ToJsonElement<short>() };
    }

    public static BuilderOp ToBuilderOp(this int i) {
        return new BuilderOp.Integer { Size = 32, Value = i.ToJsonElement<int>() };
    }

    public static BuilderOp ToBuilderOp(this long l) {
        return new BuilderOp.Integer { Size = 64, Value = l.ToJsonElement<long>() };
    }

    public static BuilderOp ToBuilderOp(this string s) {
        return new BuilderOp.BitString { Value = s };
    }

    public static BuilderOp ToBuilderOpCellBoc(this string s) {
        return new BuilderOp.CellBoc { Boc = s };
    }
}