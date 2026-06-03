/* eslint-disable @typescript-eslint/no-explicit-any */
export const isOfType = <T extends new (...args: any[]) => any>(
    obj: any,
    type: T,
): obj is T => {
    // Ensure 'type' is a function and a constructor
    return (
        typeof type === 'function' &&
        type.prototype instanceof Object &&
        obj instanceof type
    );
};
