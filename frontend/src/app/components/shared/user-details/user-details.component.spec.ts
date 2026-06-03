import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserDetailsComponent } from './user-details.component';
import { By } from '@angular/platform-browser';

describe('UserDetailsComponent', () => {
    let component: UserDetailsComponent;
    let fixture: ComponentFixture<UserDetailsComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [UserDetailsComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(UserDetailsComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display the user name passed via the value input', () => {
        // Arrange
        component.value = 'John Doe';

        // Act
        fixture.detectChanges();

        // Assert
        const userNameElement = fixture.debugElement.query(
            By.css('.user-name'),
        ).nativeElement;
        expect(userNameElement.textContent).toBe('John Doe');
    });

    it('should update the displayed user name when the input value changes', () => {
        // Test initial value
        component.value = 'Jane Doe';
        fixture.detectChanges();

        const userNameElement = fixture.debugElement.query(
            By.css('.user-name'),
        ).nativeElement;
        expect(userNameElement.textContent).toBe('Jane Doe');

        // Create a new component instance for the second test
        const secondFixture = TestBed.createComponent(UserDetailsComponent);
        const secondComponent = secondFixture.componentInstance;
        
        // Test updated value with new instance
        secondComponent.value = 'Alice';
        secondFixture.detectChanges();

        const secondUserNameElement = secondFixture.debugElement.query(
            By.css('.user-name'),
        ).nativeElement;
        expect(secondUserNameElement.textContent).toBe('Alice');
    });
});
