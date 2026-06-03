import { TestBed } from '@angular/core/testing';
import { VisibleOnlyPipe } from './visibleOnly';

interface INameAndVisible {
    name: string;
    visible: boolean;
}

describe('VisibleOnlyPipe', () => {
    let pipe: VisibleOnlyPipe<INameAndVisible>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [VisibleOnlyPipe],
        });
        pipe = TestBed.inject(VisibleOnlyPipe);
    });

    it('should be created', () => {
        expect(pipe).toBeTruthy();
    });

    it('should filter an array of objects based on the callback function', () => {
        const items = [
            { name: 'Alice', visible: true },
            { name: 'Bob', visible: false },
            { name: 'Charlie', visible: true },
        ];
        const callback = (item: INameAndVisible) => item.visible;

        const result = pipe.transform(items, callback);

        expect(result).toEqual([
            { name: 'Alice', visible: true },
            { name: 'Charlie', visible: true },
        ]);
    });

    it('should return an empty array if no items match the callback', () => {
        const items = [
            { name: 'Alice', visible: false },
            { name: 'Bob', visible: false },
        ];
        const callback = (item: INameAndVisible) => item.visible;

        const result = pipe.transform(items, callback);

        expect(result).toEqual([]);
    });

    it('should return all items if all items match the callback', () => {
        const items = [
            { name: 'Alice', visible: true },
            { name: 'Bob', visible: true },
        ];
        const callback = (item: INameAndVisible) => item.visible;

        const result = pipe.transform(items, callback);

        expect(result).toEqual(items);
    });

    it('should return an empty array if the input array is empty', () => {
        const items: INameAndVisible[] = [];
        const callback = (item: INameAndVisible) => item.visible;

        const result = pipe.transform(items, callback);

        expect(result).toEqual([]);
    });

    it('should handle cases where the callback returns false for all items', () => {
        const items = [
            { name: 'Alice', visible: true },
            { name: 'Bob', visible: true },
        ];
        const callback = () => false;

        const result = pipe.transform(items, callback);

        expect(result).toEqual([]);
    });

    [
        {
            value: undefined,
            text: 'undefined',
        },
        {
            value: null,
            text: 'null',
        },
    ].forEach(({ value, text }) => {
        it(`should not throw error if the input is ${text}`, () => {
            const callback = (item: INameAndVisible) => item.visible;

            const result = pipe.transform(value, callback);

            expect(result).toEqual(undefined);
        });
    });
});
