import 'reflect-metadata';
const requiredMetadataKey = Symbol('required');

export const required = (
    target: object,
    propertyKey: string | symbol,
    parameterIndex: number,
) => {
    const existingRequiredParameters: number[] =
        Reflect.getOwnMetadata(requiredMetadataKey, target, propertyKey) || [];
    existingRequiredParameters.push(parameterIndex);
    Reflect.defineMetadata(
        requiredMetadataKey,
        existingRequiredParameters,
        target,
        propertyKey,
    );
};
export const validate = <T, TArg>(
    target: T,
    propertyName: string,
    descriptor: TypedPropertyDescriptor<(...args: TArg[]) => void>,
) => {
    const method = descriptor.value;

    descriptor.value = function (...args: TArg[]) {
        const requiredParameters: number[] = Reflect.getOwnMetadata(
            requiredMetadataKey,
            target,
            propertyName,
        );
        if (requiredParameters) {
            for (const parameterIndex of requiredParameters) {
                if (parameterIndex >= args.length || !args[parameterIndex]) {
                    throw new Error('Missing required argument.');
                }
            }
        }
        return method.apply(this, args);
    };
};
