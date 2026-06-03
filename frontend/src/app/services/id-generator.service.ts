import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class IdGeneratorService {
    private counter = 0;

    getNextFakeId(): number {
        return --this.counter;
    }

    isFake(value: string): boolean {
        return !!Number(value) && parseInt(value) < 0;
    }
}
