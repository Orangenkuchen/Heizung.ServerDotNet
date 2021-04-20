CREATE OR REPLACE
VIEW `OperatingHoures` AS
select
    `SubTable`.`Timestamp` AS `Timestamp`,
    `SubTable`.`MaxDay` AS `MaxDay`,
    `SubTable`.`MinDay` AS `MinDay`,
    `SubTable`.`MaxDay` - `SubTable`.`MinDay` AS `Amount`
from
    (
    select
        `Heizung`.`DataValues`.`Timestamp` AS `Timestamp`,
        max(`Heizung`.`DataValues`.`Value`) AS `MaxDay`,
        min(`Heizung`.`DataValues`.`Value`) AS `MinDay`
    from
        `Heizung`.`DataValues`
    where
        `Heizung`.`DataValues`.`ValueType` = 30
    group by
        cast(`Heizung`.`DataValues`.`Timestamp` as date)
    order by
        `Heizung`.`DataValues`.`Timestamp`) `SubTable` WITH LOCAL CHECK OPTION