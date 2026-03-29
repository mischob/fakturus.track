import SwiftUI

struct SchoolHolidaysScreen: View {
    @Bindable var viewModel: SettingsViewModel
    @State private var showAddSheet = false
    @State private var editingHoliday: SchoolHolidayPeriod?

    // Add form state
    @State private var formName = ""
    @State private var formStartDate = Date()
    @State private var formEndDate = Date()

    var body: some View {
        List {
            if viewModel.schoolHolidays.isEmpty {
                Section {
                    VStack(spacing: 12) {
                        Image(systemName: "sun.max.trianglebadge.exclamationmark")
                            .font(.largeTitle)
                            .foregroundStyle(.secondary)
                        Text("Keine Schulferien eingetragen")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 32)
                }
            } else {
                Section {
                    ForEach(viewModel.schoolHolidays) { period in
                        Button {
                            formName = period.name
                            formStartDate = period.startDate
                            formEndDate = period.endDate
                            editingHoliday = period
                        } label: {
                            VStack(alignment: .leading, spacing: 4) {
                                Text(period.name)
                                    .font(.body)
                                    .foregroundStyle(Theme.textPrimary)
                                Text("\(formatted(period.startDate)) – \(formatted(period.endDate))")
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                        }
                    }
                    .onDelete { indexSet in
                        for index in indexSet {
                            viewModel.deleteSchoolHoliday(viewModel.schoolHolidays[index])
                        }
                    }
                }
            }
        }
        .listStyle(.insetGrouped)
        .navigationTitle("Schulferien")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    formName = ""
                    formStartDate = Date()
                    formEndDate = Calendar.current.date(byAdding: .weekOfYear, value: 1, to: Date()) ?? Date()
                    showAddSheet = true
                } label: {
                    Image(systemName: "plus")
                }
            }
        }
        .sheet(isPresented: $showAddSheet) {
            schoolHolidayForm(isEditing: false)
        }
        .sheet(item: $editingHoliday) { period in
            schoolHolidayForm(isEditing: true, existing: period)
        }
    }

    @ViewBuilder
    private func schoolHolidayForm(isEditing: Bool, existing: SchoolHolidayPeriod? = nil) -> some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Name (z.B. Sommerferien)", text: $formName)
                }

                Section {
                    DatePicker("Von", selection: $formStartDate, displayedComponents: .date)
                    DatePicker("Bis", selection: $formEndDate, displayedComponents: .date)
                }
            }
            .navigationTitle(isEditing ? "Bearbeiten" : "Neue Schulferien")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") {
                        showAddSheet = false
                        editingHoliday = nil
                    }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern") {
                        if let existing {
                            viewModel.updateSchoolHoliday(existing, name: formName, startDate: formStartDate, endDate: formEndDate)
                            editingHoliday = nil
                        } else {
                            viewModel.addSchoolHoliday(name: formName, startDate: formStartDate, endDate: formEndDate)
                            showAddSheet = false
                        }
                    }
                    .disabled(formName.trimmingCharacters(in: .whitespaces).isEmpty)
                }
            }
        }
        .presentationDetents([.medium])
    }

    private func formatted(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "de_DE")
        formatter.dateStyle = .medium
        return formatter.string(from: date)
    }
}
