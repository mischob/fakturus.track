package com.fakturus.track.ui.shared

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.fakturus.track.util.HolidayCalculator
import java.time.LocalDate

data class BundeslandEntry(val code: String, val name: String)

val bundeslaender = listOf(
    BundeslandEntry("BW", "Baden-Wuerttemberg"),
    BundeslandEntry("BY", "Bayern"),
    BundeslandEntry("BE", "Berlin"),
    BundeslandEntry("BB", "Brandenburg"),
    BundeslandEntry("HB", "Bremen"),
    BundeslandEntry("HH", "Hamburg"),
    BundeslandEntry("HE", "Hessen"),
    BundeslandEntry("MV", "Mecklenburg-Vorpommern"),
    BundeslandEntry("NI", "Niedersachsen"),
    BundeslandEntry("NW", "Nordrhein-Westfalen"),
    BundeslandEntry("RP", "Rheinland-Pfalz"),
    BundeslandEntry("SL", "Saarland"),
    BundeslandEntry("SN", "Sachsen"),
    BundeslandEntry("ST", "Sachsen-Anhalt"),
    BundeslandEntry("SH", "Schleswig-Holstein"),
    BundeslandEntry("TH", "Thueringen"),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BundeslandPicker(
    selectedBundesland: String,
    onBundeslandChange: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    var expanded by remember { mutableStateOf(false) }
    val currentYear = remember { LocalDate.now().year }
    val selected = bundeslaender.firstOrNull { it.code == selectedBundesland }
    val holidayCount = remember(selectedBundesland, currentYear) {
        HolidayCalculator.holidayCount(selectedBundesland, currentYear)
    }

    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { expanded = !expanded },
        modifier = modifier
    ) {
        OutlinedTextField(
            value = selected?.name ?: selectedBundesland,
            onValueChange = {},
            readOnly = true,
            label = { Text("Bundesland") },
            supportingText = { Text("$holidayCount Feiertage in $currentYear") },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            modifier = Modifier
                .fillMaxWidth()
                .menuAnchor(MenuAnchorType.PrimaryNotEditable)
        )

        ExposedDropdownMenu(
            expanded = expanded,
            onDismissRequest = { expanded = false }
        ) {
            bundeslaender.forEach { entry ->
                val count = remember(entry.code, currentYear) {
                    HolidayCalculator.holidayCount(entry.code, currentYear)
                }
                DropdownMenuItem(
                    text = {
                        Text("${entry.name} ($count)")
                    },
                    onClick = {
                        onBundeslandChange(entry.code)
                        expanded = false
                    },
                    contentPadding = ExposedDropdownMenuDefaults.ItemContentPadding
                )
            }
        }
    }
}
