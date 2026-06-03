import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'visibleOnly',
    standalone: false,
})
export class VisibleOnlyPipe<T> implements PipeTransform {
    transform(items: T[], callback: (item: T) => boolean): T[] {
        return items?.filter((item) => callback(item));
    }
}
