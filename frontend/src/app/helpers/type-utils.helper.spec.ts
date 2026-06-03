import { describe, expect, it } from 'vitest';
import { isOfType } from './type-utils.helper';

class ExampleClass {}
class AnotherClass {}

describe('isOfType', () => {
    it('should return true for instances of the specified class', () => {
        const exampleInstance = new ExampleClass();

        expect(isOfType(exampleInstance, ExampleClass)).toBe(true);
    });

    it('should return false for instances of different classes', () => {
        const exampleInstance = new ExampleClass();
        const anotherInstance = new AnotherClass();

        expect(isOfType(exampleInstance, AnotherClass)).toBe(false);
        expect(isOfType(anotherInstance, ExampleClass)).toBe(false);
    });

    it('should return false for non-class instances', () => {
        const nonClassInstance = {};

        expect(isOfType(nonClassInstance, ExampleClass)).toBe(false);
    });

    it('should handle null and undefined values', () => {
        expect(isOfType(null, ExampleClass)).toBe(false);
        expect(isOfType(undefined, ExampleClass)).toBe(false);
    });

    it('should return false if the type is not a constructor', () => {
        const exampleInstance = new ExampleClass();
        const notAConstructor = 123;

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        expect(isOfType(exampleInstance, notAConstructor as any)).toBe(false);
    });
});
