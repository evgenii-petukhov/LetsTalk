import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UnreadCountComponent } from './unread-count.component';

describe('UnreadCountComponent', () => {
    let component: UnreadCountComponent;
    let fixture: ComponentFixture<UnreadCountComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [UnreadCountComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(UnreadCountComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
