import { Pipe, PipeTransform } from '@angular/core';
import { List, ListIteratee, Many, orderBy } from 'lodash-es';

@Pipe({
    name: 'orderBy',
    standalone: false,
})
export class OrderByPipe<T> implements PipeTransform {
    transform(
        array: List<T>,
        sortBy: Many<ListIteratee<T>>,
        order: Many<boolean | 'asc' | 'desc'>,
    ): T[] {
        return orderBy(array, sortBy, order);
    }
}
